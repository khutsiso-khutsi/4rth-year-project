// MERGE these actions into your existing TechnicianDashboardController.
// Keep your existing [Authorize] attribute, ViewBag.Email assignment, and the other actions
// (ReceiveSamples, CaptureResults, Reports, Profile).
// NOTE: "Verification" below is the RESULT verification list. Remove your old placeholder Verification action.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using _4th_year_set_up.Models;

public class TechnicianDashboardController : Controller
{
    public IActionResult Index()
    {
        ViewBag.Email = User?.Identity?.Name;           // keep whatever you already use to set this
        return View(DashboardViewModel.Build());
    }

    // ---------------------------------------------------------------- test lists

    // Tests with a verified sample that technicians are working through.
    // Optional ?filter=urgent|overdue|nearing pre-selects a filter chip.
    public IActionResult TestQueue(string filter = "all")
        => List("queue", "Testing Queue", "Tests with a verified sample that still need work.",
                TestData.All.Where(t => t.IsInQueue), filter);

    public IActionResult MyTests(string filter = "all")
        => List("mine", "My Tests", "Tests you have selected, including any returned to you for review.",
                TestData.All.Where(t => t.IsInQueue && t.AssignedToMe), filter);

    // Sample already verified by a senior technician, waiting for a technician to pick it up.
    public IActionResult AwaitingAssignment(string filter = "all")
        => List("awaiting", "Awaiting Assignment", "Samples verified by a senior technician. Pick one to start.",
                TestData.All.Where(t => t.Stage == TestStage.AwaitingAssignment), filter);

    // Results that have been verified and released.
    public IActionResult Verified(string filter = "all")
        => List("verified", "Verified Tests", "Results verified by a senior technician and released.",
                TestData.All.Where(t => t.Stage == TestStage.Verified), filter);

    // ---------------------------------------------------------------- senior technician: step 2, sample verification

    public IActionResult SampleVerification(string filter = "all")
    {
        if (!CurrentUser.CanVerifySample) return NotSenior();
        return List("samples", "Sample Verification", "Received samples waiting for a senior technician to accept or reject.",
                    TestData.All.Where(t => t.Stage == TestStage.AwaitingSampleVerification), filter);
    }

    public IActionResult VerifySample(string id)
    {
        if (!CurrentUser.CanVerifySample) return NotSenior();
        var item = Find(id);
        if (item == null) return NotFound();
        ViewBag.Email = User?.Identity?.Name;
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AcceptSample(string id, string[] checks)
    {
        if (!CurrentUser.CanVerifySample) return NotSenior();
        var item = Find(id);
        if (item == null) return NotFound();
        if (item.Stage != TestStage.AwaitingSampleVerification)
        {
            Flash($"{id} is no longer waiting for sample verification.", "warn");
            return RedirectToAction("SampleVerification");
        }
        if ((checks?.Length ?? 0) < SampleChecklist.Items.Length)
        {
            Flash("Tick every check before accepting the sample, or reject it with a reason.", "warn");
            return RedirectToAction("VerifySample", new { id });
        }

        item.Stage = TestStage.AwaitingAssignment;
        item.SampleCondition = "Acceptable";
        item.SampleVerifiedBy = CurrentUser.Id;
        item.SampleVerifiedAt = DateTime.Now;
        item.Log("Sample verified", CurrentUser.Id, EventScope.Sample, EventTone.Good, detail: $"All {SampleChecklist.Items.Length} checks passed");
        Flash($"{id}: sample accepted. The test is now awaiting assignment.", "ok");
        return RedirectToAction("SampleVerification");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RejectSample(string id, string reasonType, string reasonNote)
    {
        if (!CurrentUser.CanVerifySample) return NotSenior();
        var item = Find(id);
        if (item == null) return NotFound();
        if (item.Stage != TestStage.AwaitingSampleVerification)
        {
            Flash($"{id} is no longer waiting for sample verification.", "warn");
            return RedirectToAction("SampleVerification");
        }
        if (string.IsNullOrWhiteSpace(reasonType))
        {
            Flash("Choose a reason for rejecting the sample.", "warn");
            return RedirectToAction("VerifySample", new { id });
        }

        item.Stage = TestStage.SampleRejected;
        item.SampleRejectReason = string.IsNullOrWhiteSpace(reasonNote)
            ? reasonType
            : $"{reasonType}: {reasonNote.Trim()}";
        item.SampleVerifiedBy = CurrentUser.Id;
        item.SampleVerifiedAt = DateTime.Now;
        item.Log("Sample rejected", CurrentUser.Id, EventScope.Sample, EventTone.Bad, detail: item.SampleRejectReason);
        Flash($"{id}: sample rejected. A recollection should be requested from {item.Ward}.", "ok");
        return RedirectToAction("SampleVerification");
    }

    // ---------------------------------------------------------------- senior technician: result verification (two-step validation)

    // Completed results. Rows you may verify are actionable; the others say why not.
    public IActionResult Verification(string filter = "all")
    {
        if (!CurrentUser.IsSenior) return NotSenior();
        return List("results", "Result Verification",
                    "Captured results waiting for a second technician to verify before release.",
                    TestData.All.Where(t => t.Stage == TestStage.Complete), filter);
    }

    public IActionResult VerifyResult(string id)
    {
        if (!CurrentUser.IsSenior) return NotSenior();
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

        item.Stage = TestStage.Verified;
        item.VerifiedBy = CurrentUser.Id;
        item.VerifiedAt = DateTime.Now;
        item.ReviewNote = null;
        item.Log("Result verified", CurrentUser.Id, tone: EventTone.Good, attempt: item.Attempt);
        Flash($"{id}: result verified and released.", "ok");
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

        item.Log("Result returned to technician", CurrentUser.Id, tone: EventTone.Warn, detail: note.Trim(), attempt: item.Attempt);
        item.History.Add(new ReturnedAttempt
        {
            Note = note.Trim(),
            CapturedBy = item.CapturedBy,
            CapturedAt = item.CapturedAt,
            Results = item.Results.Select(r => new ResultLine { Parameter = r.Parameter, Value = r.Value, Unit = r.Unit, Range = r.Range, Flag = r.Flag }).ToList()
        });
        item.Stage = TestStage.ToBeReviewed;
        item.ReviewNote = note.Trim();
        item.AssignedToMe = false;   // goes back to the technician who captured it
        Flash($"{id}: returned to technician {item.CapturedBy} for review.", "ok");
        return RedirectToAction("Verification");
    }

    // ---------------------------------------------------------------- technician: capture results

    // No id: your tests you can capture. With an id: the capture screen for that test.
    // Review & re-enter (status To Be Reviewed) opens the same screen with the returned note shown at the top.
    public IActionResult CaptureResults(string id, string filter = "all")
    {
        if (string.IsNullOrEmpty(id))
            return List("mine", "Capture Results", "Tests you have selected. Open one to capture its result.",
                        TestData.All.Where(t => t.IsInQueue && t.AssignedToMe), filter);

        var item = Find(id);
        if (item == null) return NotFound();

        if (!CanCapture(item))
        {
            Flash($"{id} isn't available for result capture. Select it from the unassigned list first.", "warn");
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
            Flash($"{id} isn't available for result capture.", "warn");
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
        if (item.Stage == TestStage.Assigned) item.Stage = TestStage.InProgress;   // saving counts as starting

        item.Results = lines;
        item.CaptureNote = comments.Count == 0 ? null : string.Join("; ", comments);

        if (!finalise)
        {
            Flash($"{id}: draft saved. It stays in your tests until you submit it.", "ok");
            return RedirectToAction("CaptureResults", new { id });
        }

        // Submit: one audit entry per submission. A re-entry shows what changed since the returned attempt.
        var changes = wasReturned ? TestItem.Diff(item.History.LastOrDefault()?.Results, lines) : null;

        item.CapturedBy = CurrentUser.Id;
        item.CapturedAt = DateTime.Now;
        item.ReviewNote = null;                 // the earlier note is kept in History
        item.Stage = TestStage.Complete;

        var detail = TestItem.Summarise(lines) + (item.CaptureNote != null ? $" — {item.CaptureNote}" : "");
        item.Log(wasReturned ? "Result re-entered" : "Result captured", CurrentUser.Id,
                 detail: detail, attempt: item.Attempt, changes: changes);

        var critical = lines.Where(l => l.Flag != null && l.Flag.StartsWith("Critical")).ToList();
        if (critical.Count > 0)
        {
            var values = string.Join(", ", critical.Select(c => $"{c.Parameter} {c.Value} {c.Unit}"));
            item.Log("Critical value flagged", CurrentUser.Id, tone: EventTone.Bad, attempt: item.Attempt,
                     detail: $"{values}. Phone {item.Ward} ({item.RequestingDoctor}).");
            Flash($"{id}: submitted for verification. CRITICAL: {values}. Phone {item.Ward} ({item.RequestingDoctor}) now.", "warn");
        }
        else
        {
            Flash($"{id}: results submitted for verification.", "ok");
        }

        return RedirectToAction("MyTests");
    }

    // ---------------------------------------------------------------- test details

    public IActionResult TestDetails(string id)
    {
        var item = Find(id);
        if (item == null) return NotFound();
        ViewBag.Email = User?.Identity?.Name;
        return View(item);
    }

    // ---------------------------------------------------------------- technician: select / release

    // NOTE: hardcoded data is shared in memory, so everything resets when the app restarts.
    // With the database, make this one atomic UPDATE ... WHERE Stage = AwaitingAssignment so two technicians can't both win.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SelectTest(string id, string returnUrl)
    {
        var item = Find(id);
        if (item == null) return NotFound();

        if (item.Stage != TestStage.AwaitingAssignment)
        {
            Flash($"{id} has already been taken by another technician.", "warn");
        }
        else
        {
            item.Stage = TestStage.Assigned;
            item.AssignedToMe = true;
            item.Log("Test picked up", CurrentUser.Id);
            Flash($"{id} is now in your tests.", "ok");
        }
        return BackTo(returnUrl);
    }

    // Puts one of your own, not-yet-started tests back in the pool.
    // Tests already in progress can't be released here; that is a supervisor reassignment.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReleaseTest(string id, string returnUrl)
    {
        var item = Find(id);
        if (item == null) return NotFound();

        if (item.AssignedToMe && item.Stage == TestStage.Assigned)
        {
            item.Stage = TestStage.AwaitingAssignment;
            item.AssignedToMe = false;
            item.Log("Returned to the pool", CurrentUser.Id);
            Flash($"{id} was returned to the pool.", "ok");
        }
        else
        {
            Flash($"{id} can't be released. Ask a supervisor to reassign it.", "warn");
        }
        return BackTo(returnUrl);
    }

    // ---------------------------------------------------------------- helpers

    // A test you hold that is ready for results (or was returned for review).
    private static bool CanCapture(TestItem t)
        => t.AssignedToMe && (t.Stage is TestStage.Assigned or TestStage.InProgress or TestStage.ToBeReviewed);

    private static TestItem Find(string id) => TestData.All.FirstOrDefault(t => t.ReqNum == id);

    private void Flash(string message, string type)
    {
        TempData["Flash"] = message;
        TempData["FlashType"] = type;
    }

    private IActionResult NotSenior()
    {
        Flash("That screen is for senior technicians.", "warn");
        return RedirectToAction("Index");
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
            Counts = DashboardViewModel.Build()
        });
    }
}
