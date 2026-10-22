using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using _4th_year_set_up.Repository;
using _4th_year_set_up.Models;

namespace _4th_year_set_up.Controllers
{
public class TechnicianDashboardController : Controller
{
    private readonly TechnicianRepository _db;

    public TechnicianDashboardController(TechnicianRepository db)
    {
        _db = db;
        // The test types the laboratory manager assigned to this technician (TechnicianTestTypes).
        CurrentUser.VerifiableTests = _db.GetAssignedTestNames(CurrentUser.Id);
    }

    public IActionResult Index()
    {
        ViewBag.Email = User?.Identity?.Name;           // keep whatever you already use to set this
        return View(DashboardViewModel.Build(_db));
    }

    // ---------------------------------------------------------------- your other screens (views you already have)
    // These just open their view. If yours does more, keep your own version of the action.

    public IActionResult ReceiveSamples() { ViewBag.Email = User?.Identity?.Name; return View(); }
    public IActionResult Reports()        { ViewBag.Email = User?.Identity?.Name; return View(); }

    // ---------------------------------------------------------------- profile

    public IActionResult Profile()
    {
        var profile = _db.GetProfile(CurrentUser.Id);
        if (profile == null) return NotFound();
        ViewBag.Email = profile.Email;
        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateProfile(string firstName, string lastName, string cellphoneNumber)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            TempData["Error"] = "First name and last name are required.";
        else if (!System.Text.RegularExpressions.Regex.IsMatch(cellphoneNumber?.Trim() ?? "", @"^0[6-8]\d{8}$"))
            TempData["Error"] = "Enter a valid 10-digit South African cellphone number.";
        else if (!_db.UpdateProfile(CurrentUser.Id, firstName, lastName, cellphoneNumber))
            TempData["Error"] = "Your profile could not be updated.";
        else
            TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            TempData["Error"] = "Enter your current password and a new password.";
        else if (newPassword != confirmPassword)
            TempData["Error"] = "The new passwords do not match.";
        else if (newPassword.Length < 8 || !newPassword.Any(char.IsUpper) || !newPassword.Any(char.IsDigit))
            TempData["Error"] = "Password must be at least 8 characters with 1 uppercase letter and 1 number.";
        else if (newPassword == currentPassword)
            TempData["Error"] = "The new password cannot be the same as your current password.";
        else
        {
            var error = _db.ChangePassword(CurrentUser.Id, currentPassword, newPassword);
            if (error == null) TempData["Success"] = "Your password has been changed.";
            else TempData["Error"] = error;
        }
        return RedirectToAction("Profile");
    }

    // ---------------------------------------------------------------- test lists

    // Tests with a verified sample that technicians are working through.
    // Optional ?filter=urgent|overdue|nearing pre-selects a filter chip.
    public IActionResult TestQueue(string filter = "all")
        => List("queue", "Testing Queue", "Tests with a verified sample that still need work.",
                _db.GetTests().Where(t => t.IsInQueue && !t.AssignedToMe), filter);

    public IActionResult MyTests(string filter = "all")
        => List("mine", "My Tests", "Tests you have selected, including any returned to you for review.",
                _db.GetTests().Where(t => t.IsInQueue && t.AssignedToMe), filter);

    // Sample already verified, waiting for a technician to pick it up.
    public IActionResult AwaitingAssignment(string filter = "all")
        => List("awaiting", "Awaiting Assignment", "Samples that have been verified. Pick one to start.",
                _db.GetTests().Where(t => t.Stage == TestStage.AwaitingAssignment), filter);

    // Results that have been verified.
    public IActionResult Verified(string filter = "all")
        => List("verified", "Verified Tests", "Results verified by a second technician. A request is complete when every test on it is verified.",
                _db.GetTests().Where(t => t.Stage == TestStage.Verified), filter);

    // ---------------------------------------------------------------- step 2, sample verification

    public IActionResult SampleVerification(string filter = "all")
    {
        // One row per tube. A tube holding several tests is verified once for all of them.
        return List("samples", "Sample Verification", "Received tubes waiting to be accepted or rejected. Each tube is verified once.",
                    _db.GetRequests().SelectMany(r => r.Tubes)
                        .Where(tube => tube.Status == TubeStatus.AwaitingVerification && tube.Tests.Count > 0)
                        .Select(tube => tube.Tests.First()), filter);
    }

    public IActionResult VerifySample(string id)
    {
        var item = Find(id);
        if (item == null) return NotFound();
        ViewBag.Email = User?.Identity?.Name;
        return View(item);
    }

    // Accepting or rejecting works on the TUBE of the test that was opened, so it covers every test on that tube.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AcceptSample(string id, string[] checks)
    {
        var item = Find(id);
        if (item == null) return NotFound();
        var tube = item.Sample;
        if (tube.Status != TubeStatus.AwaitingVerification)
        {
            Flash($"{tube.TubeId} is no longer waiting for sample verification.", "warn");
            return RedirectToAction("SampleVerification");
        }
        if ((checks?.Length ?? 0) < SampleChecklist.Items.Length)
        {
            Flash("Tick every check before accepting the sample, or reject it with a reason.", "warn");
            return RedirectToAction("VerifySample", new { id });
        }

        var ev = tube.Log("Sample verified", CurrentUser.Id, EventTone.Good, $"All {SampleChecklist.Items.Length} checks passed");
        if (!_db.AcceptSample(tube, CurrentUser.Id, ev))
        {
            Flash($"{tube.TubeId} is no longer waiting for sample verification.", "warn");
            return RedirectToAction("SampleVerification");
        }

        Flash($"{item.ReqNum}: tube accepted. {tube.TestsLabel} {(tube.Tests.Count == 1 ? "is" : "are")} now awaiting assignment.", "ok");
        return RedirectToAction("SampleVerification");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RejectSample(string id, string reasonType, string reasonNote)
    {
        var item = Find(id);
        if (item == null) return NotFound();
        var tube = item.Sample;
        if (tube.Status != TubeStatus.AwaitingVerification)
        {
            Flash($"{tube.TubeId} is no longer waiting for sample verification.", "warn");
            return RedirectToAction("SampleVerification");
        }
        if (string.IsNullOrWhiteSpace(reasonType))
        {
            Flash("Choose a reason for rejecting the sample.", "warn");
            return RedirectToAction("VerifySample", new { id });
        }

        var reason = string.IsNullOrWhiteSpace(reasonNote)
            ? reasonType
            : $"{reasonType}: {reasonNote.Trim()}";

        var ev = tube.Log("Sample rejected", CurrentUser.Id, EventTone.Bad, reason);
        if (!_db.RejectSample(tube, CurrentUser.Id, reasonType, reason, ev))
        {
            Flash($"{tube.TubeId} is no longer waiting for sample verification.", "warn");
            return RedirectToAction("SampleVerification");
        }

        Flash($"{item.ReqNum}: tube rejected ({tube.TestsLabel}). A recollection should be requested from {item.Ward}.", "ok");
        return RedirectToAction("SampleVerification");
    }

    // ---------------------------------------------------------------- result verification (two-step validation)

    // Completed results. Rows you may verify are actionable; the others say why not.
    public IActionResult Verification(string filter = "all")
    {
        return List("results", "Result Verification",
                    "Captured results waiting for a second technician to verify before release.",
                    _db.GetTests().Where(t => t.Stage == TestStage.Complete), filter);
    }

    public IActionResult VerifyResult(string id)
    {
        var item = Find(id);
        if (item == null) return NotFound();
        ViewBag.Email = User?.Identity?.Name;
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApproveResult(string id)
    {
        var item = Find(id);
        if (item == null) return NotFound();

        var why = CurrentUser.WhyCannotVerifyResult(item);
        if (why != null)
        {
            Flash(why, "warn");
            return RedirectToAction("Verification");
        }

        var ev = item.Log("Result verified", CurrentUser.Id, tone: EventTone.Good, attempt: item.Attempt);
        if (!_db.ApproveResult(item, CurrentUser.Id, ev))
        {
            Flash($"{Label(item)} can't be verified. It is no longer waiting for verification, or you captured it yourself.", "warn");
            return RedirectToAction("Verification");
        }

        item.Stage = TestStage.Verified;     // so the message below reflects the request as it is now
        Flash(item.Request.IsReadyToRelease
                ? $"{Label(item)}: result verified. Every test on {item.ReqNum} is now verified, so the request is complete and ready for the doctor to release."
                : $"{Label(item)}: result verified." + (item.Request.HoldReason != null ? $" {item.ReqNum} stays open: {item.Request.HoldReason}." : ""), "ok");
        return RedirectToAction("Verification");
    }

    // Not verified: returned to the technician who performed the test, with a note. Status becomes To Be Reviewed.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReturnResult(string id, string note)
    {
        var item = Find(id);
        if (item == null) return NotFound();

        var why = CurrentUser.WhyCannotVerifyResult(item);
        if (why != null)
        {
            Flash(why, "warn");
            return RedirectToAction("Verification");
        }
        if (string.IsNullOrWhiteSpace(note))
        {
            Flash("Add a note explaining why the result is being returned.", "warn");
            return RedirectToAction("VerifyResult", new { id });
        }

        var ev = item.Log("Result returned to technician", CurrentUser.Id, tone: EventTone.Warn, detail: note.Trim(), attempt: item.Attempt);
        if (!_db.ReturnResult(item, CurrentUser.Id, note.Trim(), ev))
        {
            Flash($"{Label(item)} is no longer waiting for verification.", "warn");
            return RedirectToAction("Verification");
        }

        Flash($"{Label(item)}: returned to {TechnicianNames.Of(item.CapturedBy)} for review. {item.ReqNum} stays open until it is verified again.", "ok");
        return RedirectToAction("Verification");
    }

    // ---------------------------------------------------------------- technician: capture results

    // No id: your tests you can capture. With an id: the capture screen for that test.
    // Review & re-enter (status To Be Reviewed) opens the same screen with the returned note shown at the top.
    public IActionResult CaptureResults(string id, string filter = "all")
    {
        if (string.IsNullOrEmpty(id))
            return List("mine", "Capture Results", "Tests you have selected. Open one to capture its result.",
                        _db.GetTests().Where(t => t.IsInQueue && t.AssignedToMe), filter);

        var item = Find(id);
        if (item == null) return NotFound();

        if (!CanCapture(item))
        {
            Flash($"{Label(item)} isn't available for result capture. Select it from the unassigned list first.", "warn");
            return RedirectToAction("MyTests");
        }

        var defs = ResultCatalog.For(item.Test, item.PatientDetails?.Sex);

        // Turn any results already saved on the test (ResultLine) back into form values.
        var existing = new List<ResultValue>();
        foreach (var def in defs)
        {
            var line = item.Results?.FirstOrDefault(l => l.Parameter == def.Name);
            if (line != null && !string.IsNullOrWhiteSpace(line.Value))
                existing.Add(new ResultValue { Key = def.Key, Name = def.Name, Unit = def.Unit, Value = line.Value });
        }

        ViewBag.Email = User?.Identity?.Name;
        return View(new CaptureResultsViewModel
        {
            Item = item,
            Parameters = defs,
            Existing = existing,
            IsReview = item.Stage == TestStage.ToBeReviewed
        });
    }

    // Posted by the capture screen. finalise=false saves a draft (not logged); finalise=true submits for verification.
    // Flags are worked out from the reference ranges here, never taken from the browser.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveResults(string id, string resultsJson, bool finalise)
    {
        var item = Find(id);
        if (item == null) return NotFound();
        if (!CanCapture(item))
        {
            Flash($"{Label(item)} isn't available for result capture.", "warn");
            return RedirectToAction("MyTests");
        }

        List<ResultValue> posted;
        try
        {
            posted = JsonSerializer.Deserialize<List<ResultValue>>(
                         string.IsNullOrWhiteSpace(resultsJson) ? "[]" : resultsJson,
                         new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ResultValue>();
        }
        catch (JsonException)
        {
            Flash("Those results couldn't be read. Please try again.", "warn");
            return RedirectToAction("CaptureResults", new { id });
        }

        var defs = ResultCatalog.For(item.Test, item.PatientDetails?.Sex);
        var lines = new List<ResultLine>();
        var comments = new List<string>();
        var problems = new List<string>();

        foreach (var def in defs)
        {
            var p = posted.FirstOrDefault(x => x.Key == def.Key);
            var raw = p?.Value?.Trim();

            if (string.IsNullOrEmpty(raw))
            {
                if (finalise) problems.Add($"{def.Name} has no result.");
                continue;
            }

            var flag = def.IsText ? "" : ResultCatalog.Flag(def, raw);
            if (flag == "invalid")
            {
                problems.Add($"{def.Name} must be a number.");
                continue;
            }

            lines.Add(new ResultLine
            {
                Parameter = def.Name,
                Value = def.IsText ? raw : ResultCatalog.Normalise(raw),
                Unit = def.Unit,
                Range = def.RangeText,
                Flag = def.IsText ? null : ResultCatalog.ToLineFlag(flag)
            });

            if (!string.IsNullOrWhiteSpace(p.Comment)) comments.Add($"{def.Name}: {p.Comment.Trim()}");
        }

        if (problems.Count > 0)
        {
            Flash(string.Join(" ", problems), "warn");
            return RedirectToAction("CaptureResults", new { id });
        }
        if (lines.Count == 0)
        {
            Flash("Enter at least one result first.", "warn");
            return RedirectToAction("CaptureResults", new { id });
        }

        var wasReturned = item.Stage == TestStage.ToBeReviewed;
        var previousAttempt = item.History.LastOrDefault()?.Results;     // what was entered before it was returned

        item.Results = lines;
        item.CaptureNote = comments.Count == 0 ? null : string.Join("; ", comments);

        // Draft: saved against this attempt, not logged. Saving counts as starting the test.
        if (!finalise)
        {
            if (!_db.SaveDraft(item, CurrentUser.Id, item.Attempt, item.CaptureNote))
            {
                Flash($"{Label(item)} isn't available for result capture.", "warn");
                return RedirectToAction("MyTests");
            }
            Flash($"{Label(item)}: draft saved. It stays in your tests until you submit it.", "ok");
            return RedirectToAction("CaptureResults", new { id });
        }

        // Submit: one audit entry per submission. A re-entry shows what changed since the returned attempt.
        var changes = wasReturned ? TestItem.Diff(previousAttempt, lines) : null;
        var detail = TestItem.Summarise(lines) + (item.CaptureNote != null ? $" — {item.CaptureNote}" : "");

        var events = new List<TestEvent>
        {
            item.Log(wasReturned ? "Result re-entered" : "Result captured", CurrentUser.Id,
                     detail: detail, attempt: item.Attempt, changes: changes)
        };

        var critical = lines.Where(l => l.Flag != null && l.Flag.StartsWith("Critical")).ToList();
        string values = null;
        if (critical.Count > 0)
        {
            values = string.Join(", ", critical.Select(c => $"{c.Parameter} {c.Value} {c.Unit}"));
            events.Add(item.Log("Critical value flagged", CurrentUser.Id, tone: EventTone.Bad, attempt: item.Attempt,
                                detail: $"{values}. Phone {item.Ward} ({item.RequestingDoctor})."));
        }

        if (!_db.SubmitResults(item, CurrentUser.Id, item.Attempt, item.CaptureNote, events))
        {
            Flash($"{Label(item)} isn't available for result capture.", "warn");
            return RedirectToAction("MyTests");
        }

        if (values != null)
            Flash($"{Label(item)}: submitted for verification. CRITICAL: {values}. Phone {item.Ward} ({item.RequestingDoctor}) now.", "warn");
        else
            Flash($"{Label(item)}: results submitted for verification.", "ok");

        return RedirectToAction("MyTests");
    }

    // ---------------------------------------------------------------- release status (read-only)

    // The doctor releases a request to the patient once every test on it is verified.
    // This page only shows which requests are ready, which are held back, and which have been released.
    public IActionResult ReleaseResults()
    {
        ViewBag.Email = User?.Identity?.Name;
        return View(ReleaseViewModel.Build(_db));
    }

    // ---------------------------------------------------------------- test details

    public IActionResult TestDetails(string id)
    {
        var item = Find(id);
        if (item == null) return NotFound();
        ViewBag.Email = User?.Identity?.Name;
        return View(item);
    }

    // ---------------------------------------------------------------- technician: select / return to pool

    // One atomic UPDATE in the database (see PickUpTest), so two technicians can't both win.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SelectTest(string id, string returnUrl)
    {
        var item = Find(id);
        if (item == null) return NotFound();

        if (item.Stage != TestStage.AwaitingAssignment)
        {
            Flash($"{Label(item)} has already been taken by another technician.", "warn");
        }
        else
        {
            var ev = item.Log("Test picked up", CurrentUser.Id);
            if (_db.PickUpTest(item, CurrentUser.Id, ev))
                Flash($"{Label(item)} is now in your tests.", "ok");
            else
                Flash($"{Label(item)} has already been taken by another technician.", "warn");
        }
        return BackTo(returnUrl);
    }

    // "Return to pool": puts one of your own, not-yet-started tests back in the pool. (Not the same as releasing results.)
    // Tests already in progress can't be returned here; that is a supervisor reassignment.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReleaseTest(string id, string returnUrl)
    {
        var item = Find(id);
        if (item == null) return NotFound();

        if (item.AssignedToMe && item.Stage == TestStage.Assigned)
        {
            var ev = item.Log("Returned to the pool", CurrentUser.Id);
            if (_db.ReturnToPool(item, CurrentUser.Id, ev))
                Flash($"{Label(item)} was returned to the pool.", "ok");
            else
                Flash($"{Label(item)} can't be returned to the pool. Ask a supervisor to reassign it.", "warn");
        }
        else
        {
            Flash($"{Label(item)} can't be returned to the pool. Ask a supervisor to reassign it.", "warn");
        }
        return BackTo(returnUrl);
    }

    // ---------------------------------------------------------------- helpers

    // A test you hold that is ready for results (or was returned for review).
    private static bool CanCapture(TestItem t)
        => t.AssignedToMe && (t.Stage is TestStage.Assigned or TestStage.InProgress or TestStage.ToBeReviewed);

    // Looks a test up by its unique TestId (the RequestItemID). A request number also works when the request has only one test.
    private TestItem Find(string id) => _db.GetTest(id);

    // "Haemoglobin on REQ-20261015-801": easier to read in a message than the test's number.
    private static string Label(TestItem t) => $"{t.Test} on {t.ReqNum}";

    private void Flash(string message, string type)
    {
        TempData["Flash"] = message;
        TempData["FlashType"] = type;
    }

    private IActionResult BackTo(string returnUrl)
        => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("MyTests");

    private IActionResult List(string mode, string title, string subtitle, IEnumerable<TestItem> items, string filter)
    {
        ViewBag.Email = User?.Identity?.Name;
        return View("TestList", new TestListViewModel
        {
            Mode = mode,
            Title = title,
            Subtitle = subtitle,
            InitialFilter = (filter ?? "all").ToLowerInvariant(),
            Items = items.ToList(),
            Counts = DashboardViewModel.Build(_db)
        });
    }
}
}
