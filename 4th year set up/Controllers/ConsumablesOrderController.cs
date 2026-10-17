@using LabManager.Models
@using System.Text.Json
@using Microsoft.AspNetCore.Mvc.Rendering
@using Microsoft.AspNetCore.Http
@model IEnumerable<ConsumableOrder>
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

@{
    ViewData["Title"] = "Consumable Orders";
    Layout = null;

    var orders = Model ?? Enumerable.Empty<ConsumableOrder>();
    var suppliers = ViewBag.Suppliers as IEnumerable<Supplier>
                    ?? Enumerable.Empty<Supplier>();

    var supplierOptions = ViewBag.Supplier as IEnumerable<SelectListItem>
                          ?? Enumerable.Empty<SelectListItem>();

    var consumableItems = ViewBag.Consumables as IEnumerable<SelectListItem>
                          ?? ViewBag.Consumable as IEnumerable<SelectListItem>
                          ?? Enumerable.Empty<SelectListItem>();

    var consumableDetails = ViewBag.ConsumableDetails ?? Enumerable.Empty<object>();

    var statusFilter = ViewBag.StatusFilter as string
                       ?? Context.Request.Query["status"].ToString();

    if (string.IsNullOrWhiteSpace(statusFilter))
    {
        statusFilter = "All Statuses";
    }

    var supplierFilter = Context.Request.Query["supplierId"].ToString();
    var email = ViewBag.Email as string ?? "";
    var successMessage = TempData["Success"] as string;
    var errorMessage = TempData["Error"] as string;
}

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Consumable Orders — NMB-HLabSys</title>

    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=DM+Serif+Display&family=DM+Sans:wght@300;400;500;600;700&display=swap"
          rel="stylesheet" />

    <style>
        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

        :root {
            --navy: #0b1f3a;
            --navy2: #122848;
            --teal: #0d9488;
            --teal-dark: #0b7c72;
            --teal-light: rgba(13,148,136,.1);
            --white: #fff;
            --offwhite: #f4f7fb;
            --muted: #6b7a99;
            --border: #dde4f0;
            --red: #dc2626;
            --red-dark: #b91c1c;
            --red-light: rgba(220,38,38,.1);
            --green: #16a34a;
            --green-dark: #128a3e;
            --green-light: rgba(22,163,74,.1);
            --amber: #d97706;
            --amber-light: rgba(217,119,6,.1);
            --purple: #4f46e5;
            --purple-light: rgba(79,70,229,.1);
            --radius: 16px;
            --shadow: 0 2px 8px rgba(11,31,58,.04);
            --shadow-lg: 0 20px 60px rgba(11,31,58,.20);
            --topbar-h: 64px;
        }

        html { -webkit-text-size-adjust: 100%; }

        body {
            font-family: 'DM Sans', sans-serif;
            background: var(--offwhite);
            color: var(--navy);
            min-height: 100vh;
            line-height: 1.5;
            -webkit-font-smoothing: antialiased;
        }

        button, input, select, textarea { font-family: inherit; font-size: inherit; color: inherit; }
        button { cursor: pointer; }
        a { color: inherit; }

        :focus-visible { outline: 2px solid var(--teal); outline-offset: 2px; }
        .form-input:focus-visible, .form-select:focus-visible, .form-textarea:focus-visible { outline: none; }

        /* ---------- Top bar ---------- */
        .topbar {
            background: var(--navy);
            padding: 0 40px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            height: var(--topbar-h);
            position: sticky;
            top: 0;
            z-index: 100;
            box-shadow: 0 2px 20px rgba(0,0,0,.15);
        }

        .brand { display: flex; align-items: center; gap: 10px; text-decoration: none; }
        .brand-icon {
            width: 34px; height: 34px; background: var(--teal); flex-shrink: 0;
            border-radius: 8px; display: flex; align-items: center; justify-content: center;
        }
        .brand-icon svg { width: 18px; height: 18px; fill: white; }
        .brand-name { font-family: 'DM Serif Display', serif; color: white; font-size: 1.1rem; line-height: 1.1; }
        .brand-name span {
            display: block; font-family: 'DM Sans', sans-serif; font-size: .6rem;
            font-weight: 300; color: rgba(255,255,255,.55); letter-spacing: .1em; text-transform: uppercase;
        }
        .topbar-right { display: flex; align-items: center; gap: 16px; }
        .user-chip {
            display: flex; align-items: center; gap: 10px;
            background: rgba(255,255,255,.07); border: 1px solid rgba(255,255,255,.1);
            border-radius: 100px; padding: 6px 14px 6px 8px;
        }
        .user-avatar {
            width: 28px; height: 28px; background: var(--teal); border-radius: 50%; flex-shrink: 0;
            display: flex; align-items: center; justify-content: center;
            font-size: .75rem; font-weight: 600; color: white;
        }
        .user-email {
            font-size: .8rem; color: rgba(255,255,255,.8);
            max-width: 160px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
        }
        .logout-btn {
            background: rgba(255,255,255,.08); border: 1px solid rgba(255,255,255,.12);
            color: rgba(255,255,255,.85); padding: 8px 16px; border-radius: 8px;
            text-decoration: none; font-size: .82rem; font-weight: 500;
            display: flex; align-items: center; gap: 6px; cursor: pointer;
            transition: background .15s, color .15s; white-space: nowrap;
        }
        .logout-btn:hover { background: rgba(255,255,255,.15); color: white; }
        .logout-btn svg { width: 14px; height: 14px; flex-shrink: 0; }

        /* ---------- Layout ---------- */
        .page { display: flex; min-height: calc(100vh - var(--topbar-h)); }
        .sidebar {
            width: 220px; background: white; border-right: 1px solid var(--border);
            padding: 24px 0; flex-shrink: 0; position: sticky; top: var(--topbar-h);
            height: calc(100vh - var(--topbar-h)); overflow-y: auto; align-self: flex-start;
        }
        .nav-section { margin-bottom: 8px; }
        .nav-label {
            font-size: .65rem; font-weight: 600; color: var(--muted);
            letter-spacing: .1em; text-transform: uppercase; padding: 0 20px; margin-bottom: 4px;
        }
        .nav-item {
            display: flex; align-items: center; gap: 10px; padding: 10px 20px;
            color: var(--muted); text-decoration: none; font-size: .85rem;
            font-weight: 500; transition: background .15s, color .15s; border-left: 3px solid transparent;
        }
        .nav-item:hover { background: var(--offwhite); color: var(--navy); }
        .nav-item.active {
            background: var(--teal-light); color: var(--teal-dark);
            border-left-color: var(--teal); font-weight: 600;
        }
        .nav-item svg { width: 16px; height: 16px; flex-shrink: 0; }

        .main { flex: 1; padding: clamp(24px, 3vw, 40px); min-width: 0; }
        .header-row {
            display: flex; justify-content: space-between; align-items: flex-start;
            margin-bottom: 24px; gap: 20px; flex-wrap: wrap;
        }
        .heading h1 { font-family: 'DM Serif Display', serif; font-size: 1.8rem; font-weight: 400; line-height: 1.2; }
        .heading p { margin-top: 6px; color: var(--muted); font-size: .85rem; font-weight: 400; }

        /* ---------- Buttons ---------- */
        .btn {
            display: inline-flex; align-items: center; justify-content: center; gap: 6px;
            border: 1px solid transparent; border-radius: 10px; padding: 10px 16px;
            cursor: pointer; font-size: .82rem; font-weight: 600; line-height: 1.2;
            text-decoration: none; white-space: nowrap;
            transition: background .18s ease, color .18s ease, border-color .18s ease, transform .18s ease;
        }
        .btn:hover { transform: translateY(-1px); }
        .btn:disabled { opacity: .6; cursor: not-allowed; transform: none; }
        .btn-primary { background: var(--teal); color: white; box-shadow: 0 2px 8px rgba(13,148,136,.25); }
        .btn-primary:hover { background: var(--teal-dark); }
        .btn-success { background: var(--green); color: white; }
        .btn-success:hover { background: var(--green-dark); }
        .btn-danger { background: var(--red); color: white; }
        .btn-danger:hover { background: var(--red-dark); }
        .btn-outline { background: white; color: var(--navy); border-color: var(--border); }
        .btn-outline:hover { border-color: var(--teal); color: var(--teal-dark); }
        .btn-small { padding: 7px 11px; font-size: .72rem; }

        /* ---------- Alerts ---------- */
        .alert {
            border-radius: 10px; padding: 13px 16px; margin-bottom: 20px;
            font-size: .82rem; font-weight: 600; display: flex;
            align-items: center; justify-content: space-between; gap: 12px;
        }
        .alert-success { background: var(--green-light); color: var(--green-dark); border: 1px solid rgba(22,163,74,.25); }
        .alert-error { background: var(--red-light); color: var(--red-dark); border: 1px solid rgba(220,38,38,.25); }
        .alert-close { background: none; border: none; cursor: pointer; font-size: 18px; line-height: 1; color: inherit; padding: 2px 6px; }

        /* ---------- Filters ---------- */
        .filter-card {
            background: white; border: 1px solid var(--border); border-radius: var(--radius);
            padding: 18px; margin-bottom: 20px; box-shadow: var(--shadow);
        }
        .filter-form { display: flex; gap: 15px; align-items: flex-end; flex-wrap: wrap; }
        .form-group { display: flex; flex-direction: column; gap: 7px; min-width: 160px; }
        .form-label { display: block; font-size: .72rem; font-weight: 700; color: var(--navy); }
        .form-input, .form-select, .form-textarea {
            border: 1px solid var(--border); border-radius: 8px; padding: 10px 12px;
            background: white; color: var(--navy); outline: none; font-size: .82rem;
            transition: border-color .15s, box-shadow .15s;
        }
        .form-input:focus, .form-select:focus, .form-textarea:focus {
            border-color: var(--teal); box-shadow: 0 0 0 3px var(--teal-light);
        }
        .form-input[readonly] { background: var(--offwhite); color: var(--muted); }
        .form-select:disabled { background: var(--offwhite); color: var(--muted); cursor: wait; }
        .form-textarea { resize: vertical; }
        .search-box { min-width: 260px; }

        /* ---------- Table ---------- */
        .table-card {
            background: white; border: 1px solid var(--border);
            border-radius: var(--radius); overflow: hidden; box-shadow: var(--shadow);
        }
        .table-header {
            padding: 17px 22px; border-bottom: 1px solid var(--border);
            display: flex; justify-content: space-between; align-items: center; gap: 12px;
        }
        .table-header h2 { font-family: 'DM Serif Display', serif; font-size: 1.05rem; font-weight: 400; }
        .table-count {
            font-size: .72rem; font-weight: 600; color: var(--muted); background: var(--offwhite);
            border: 1px solid var(--border); padding: 5px 10px; border-radius: 100px; white-space: nowrap;
        }
        .table-wrapper { width: 100%; overflow-x: auto; -webkit-overflow-scrolling: touch; }
        table { width: 100%; border-collapse: collapse; min-width: 950px; }
        th {
            background: var(--offwhite); color: var(--muted); font-size: .7rem; font-weight: 600;
            text-transform: uppercase; letter-spacing: .08em; text-align: left;
            padding: 12px 18px; border-bottom: 1px solid var(--border); white-space: nowrap;
        }
        td {
            padding: 15px 18px; border-bottom: 1px solid var(--offwhite);
            font-size: .83rem; vertical-align: middle; color: var(--navy);
        }
        tbody tr:last-child td { border-bottom: none; }
        tbody tr:hover { background: var(--offwhite); }
        .order-number, .supplier-name { font-weight: 700; }
        .items { color: var(--muted); max-width: 320px; line-height: 1.5; overflow-wrap: anywhere; }
        .date { color: var(--muted); white-space: nowrap; }
        .status {
            display: inline-flex; align-items: center; padding: 4px 10px;
            border-radius: 100px; font-size: .7rem; font-weight: 700; white-space: nowrap;
        }
        .status-ordered { color: var(--purple); background: var(--purple-light); }
        .status-partial { color: var(--amber); background: var(--amber-light); }
        .status-complete { color: var(--green-dark); background: var(--green-light); }
        .status-cancelled { color: var(--red-dark); background: var(--red-light); }
        .actions { display: flex; gap: 7px; flex-wrap: wrap; align-items: center; }
        .action-note { font-size: .75rem; font-weight: 600; }
        .action-note-complete { color: var(--green-dark); }
        .action-note-cancelled { color: var(--red-dark); }
        .empty-state { text-align: center; padding: 60px 20px; color: var(--muted); }
        .empty-icon {
            width: 70px; height: 70px; margin: 0 auto 17px; border-radius: 50%;
            background: var(--teal-light); display: flex; align-items: center; justify-content: center; font-size: 32px;
        }
        .empty-state h3 { font-size: 1rem; font-weight: 700; color: var(--navy); margin-bottom: 5px; }
        .empty-state p { font-size: .8rem; }

        /* ---------- Modals ---------- */
        .modal-overlay {
            position: fixed; inset: 0; background: rgba(11,31,58,.45);
            -webkit-backdrop-filter: blur(3px); backdrop-filter: blur(3px);
            display: none; align-items: center;
            justify-content: center; z-index: 1000; padding: 20px;
        }
        .modal-overlay.show { display: flex; }
        .modal {
            width: 100%; max-width: 560px; background: white; border-radius: var(--radius);
            box-shadow: var(--shadow-lg); max-height: 90vh; overflow-y: auto; animation: modalIn .18s ease;
        }
        .modal-large { max-width: 700px; }
        @@keyframes modalIn {
            from { opacity: 0; transform: scale(.96) translateY(8px); }
            to { opacity: 1; transform: scale(1) translateY(0); }
        }
        .modal-header {
            padding: 19px 22px; border-bottom: 1px solid var(--border);
            display: flex; justify-content: space-between; align-items: center; gap: 12px;
        }
        .modal-title { font-family: 'DM Serif Display', serif; font-size: 1.2rem; font-weight: 400; }
        .modal-close {
            width: 32px; height: 32px; border: none; background: var(--offwhite); flex-shrink: 0;
            border-radius: 8px; font-size: 20px; line-height: 1; color: var(--muted); cursor: pointer;
            transition: background .15s, color .15s;
        }
        .modal-close:hover { background: var(--border); color: var(--navy); }
        .modal-body { padding: 22px; }
        .modal-footer {
            padding: 16px 22px; border-top: 1px solid var(--border);
            display: flex; justify-content: flex-end; gap: 10px; background: var(--offwhite);
            border-radius: 0 0 var(--radius) var(--radius); flex-wrap: wrap;
        }
        .modal-subtitle { color: var(--muted); font-size: .82rem; margin: 5px 0 20px; }
        .modal-subtitle.flush { margin: 0; }
        .modal-subtitle.tight { margin: 0 0 12px; }
        .modal-form-group { margin-bottom: 17px; }
        .modal-form-group .form-label { margin-bottom: 7px; }
        .modal-form-group .form-input,
        .modal-form-group .form-select,
        .modal-form-group .form-textarea { width: 100%; }
        .field-hint { display: block; margin-top: 5px; color: var(--muted); font-size: .72rem; }
        .required { color: var(--red); }

        .cancel-warning {
            background: var(--red-light); color: var(--red-dark); border: 1px solid rgba(220,38,38,.25);
            padding: 13px; border-radius: 10px; margin-bottom: 18px; font-size: .82rem;
        }
        .cancel-warning strong { display: block; margin-top: 5px; }

        .receive-intro { font-size: .82rem; color: var(--muted); margin-bottom: 15px; }
        .receive-info { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin-bottom: 20px; }
        .info-box { background: var(--offwhite); border: 1px solid var(--border); padding: 12px; border-radius: 10px; min-width: 0; }
        .info-label { font-size: .65rem; text-transform: uppercase; letter-spacing: .06em; color: var(--muted); margin-bottom: 4px; }
        .info-value { font-size: .83rem; font-weight: 700; overflow-wrap: anywhere; }
        .receive-item {
            border: 1px solid var(--border); border-radius: 10px; padding: 13px; margin-bottom: 10px;
            display: grid; grid-template-columns: 1fr 140px; gap: 15px; align-items: center;
        }
        .receive-item > div:empty { display: none; }
        .receive-item .receive-quantity { width: 100%; }
        .receive-item-name { font-weight: 700; font-size: .83rem; }
        .receive-item-qty { font-size: .75rem; color: var(--muted); }
        .receive-item-qty, .receive-item-name { overflow-wrap: anywhere; }
        .receive-item .form-label { margin-bottom: 6px; }
        .form-input:disabled { background: var(--offwhite); color: var(--muted); cursor: not-allowed; }
        .receive-status { padding: 15px; font-size: .82rem; }
        .receive-status.error { color: var(--red-dark); }

        /* ---------- Create-order item rows ---------- */
        .order-items-panel { border: 1px solid var(--border); border-radius: 14px; overflow: hidden; background: #fbfcff; }
        .order-items-heading {
            display: grid; grid-template-columns: minmax(0,1fr) 125px 84px; gap: 12px;
            padding: 10px 14px; background: #f0f4fa; border-bottom: 1px solid var(--border);
            color: var(--muted); font-size: .69rem; font-weight: 700; letter-spacing: .07em; text-transform: uppercase;
        }
        #order-items-container { display: grid; gap: 10px; padding: 12px; }
        .order-item-row {
            display: grid !important; grid-template-columns: minmax(0,1fr) 125px 84px;
            align-items: center !important; gap: 12px !important; margin: 0 !important;
            padding: 12px; background: white; border: 1px solid var(--border);
            border-radius: 12px; box-shadow: 0 2px 5px rgba(11,31,58,.025);
            transition: border-color .15s, box-shadow .15s;
        }
        .order-item-row:focus-within { border-color: rgba(13,148,136,.65); box-shadow: 0 0 0 3px rgba(13,148,136,.08); }
        .order-item-main { min-width: 0; display: flex; align-items: flex-start; gap: 10px; }
        .order-item-number {
            display: inline-flex; flex: 0 0 30px; width: 30px; height: 30px;
            align-items: center; justify-content: center; border-radius: 9px;
            color: var(--teal-dark); background: var(--teal-light); font-size: .78rem; font-weight: 700;
        }
        .order-item-fields { min-width: 0; flex: 1; }
        .order-item-fields .form-select { width: 100%; min-width: 0; text-overflow: ellipsis; }
        .order-item-hint { display: block; margin-top: 5px; color: var(--muted); font-size: .72rem; line-height: 1.4; }
        .order-item-hint.has-selection { color: var(--teal-dark); }
        .quantity-cell label { display: block; margin-bottom: 7px; font-size: .72rem; font-weight: 700; }
        .quantity-stepper {
            display: grid; grid-template-columns: 30px minmax(35px,1fr) 30px;
            border: 1px solid var(--border); border-radius: 10px; overflow: hidden; background: white;
        }
        .quantity-stepper:focus-within { border-color: var(--teal); box-shadow: 0 0 0 3px var(--teal-light); }
        .quantity-input {
            width: 100%; min-width: 0; text-align: center; font-weight: 700;
            border: 0 !important; border-radius: 0 !important; padding: 9px 2px !important;
            box-shadow: none !important; -moz-appearance: textfield; appearance: textfield;
        }
        .quantity-input::-webkit-outer-spin-button,
        .quantity-input::-webkit-inner-spin-button { -webkit-appearance: none; margin: 0; }
        .quantity-step-btn {
            border: 0; background: #f0f7f7; color: var(--teal-dark); font-size: 1.1rem; font-weight: 700;
            cursor: pointer; transition: background .15s;
        }
        .quantity-step-btn:hover { background: var(--teal-light); }
        .remove-item-btn { width: 100%; min-width: 0; padding-left: 8px !important; padding-right: 8px !important; }
        .order-items-footer { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 0 14px 14px; flex-wrap: wrap; }
        .order-items-count { color: var(--muted); font-size: .76rem; }
        .order-supplier-note {
            display: flex; gap: 9px; align-items: flex-start; padding: 11px 12px; margin-top: 10px;
            border-radius: 10px; background: var(--teal-light); color: var(--navy2); font-size: .77rem;
        }

        /* ---------- Logout modal ---------- */
        #logout-modal {
            display: none; position: fixed; inset: 0; z-index: 1100; padding: 20px;
            background: rgba(11,31,58,.45); align-items: center; justify-content: center;
        }
        .logout-dialog {
            background: white; border-radius: 20px; padding: 32px 28px;
            max-width: 380px; width: 100%; box-shadow: 0 20px 60px rgba(11,31,58,.2);
        }
        .logout-dialog h3 { font-family: 'DM Serif Display', serif; font-size: 1.2rem; font-weight: 400; color: var(--navy); margin-bottom: 8px; }
        .logout-dialog p { font-size: .85rem; color: var(--muted); margin-bottom: 24px; line-height: 1.6; }
        .logout-actions { display: flex; gap: 10px; }
        .logout-actions .btn { flex: 1; }

        /* ---------- Responsive ---------- */
        @@media (max-width: 768px) {
            .topbar { padding: 0 20px; }
            .user-email { display: none; }
            .user-chip { padding: 6px; }
            .sidebar { display: none; }
            .main { padding: 20px 16px; }
            .header-row { flex-direction: column; align-items: flex-start; }
            .receive-item { grid-template-columns: 1fr; }
            .receive-info { grid-template-columns: 1fr; }
            .filter-form .form-group { flex: 1 1 100%; }
            .filter-form .btn { flex: 1 1 auto; }
            .modal-body { padding: 18px; }
        }
        @@media (max-width: 600px) {
            .order-items-heading { display: none; }
            .order-item-row { grid-template-columns: minmax(0,1fr) 84px !important; gap: 10px !important; }
            .order-item-main { grid-column: 1 / -1; }
            .quantity-cell { grid-column: 1; }
            .remove-item-btn { grid-column: 2; align-self: end; }
            .order-items-footer { align-items: flex-start; flex-direction: column; }
            .search-box { min-width: 0; width: 100%; }
            .logout-actions { flex-direction: column-reverse; }
        }
        @@media (prefers-reduced-motion: reduce) {
            *, *::before, *::after { animation: none !important; transition: none !important; }
            .btn:hover { transform: none; }
        }
    </style>
</head>

<body>
    <div class="topbar">
        <a class="brand" asp-controller="Home" asp-action="Index">
            <div class="brand-icon">
                <svg viewBox="0 0 24 24" aria-hidden="true">
                    <path d="M12 2a1 1 0 0 1 1 1v4h4a1 1 0 0 1 0 2h-4v4a1 1 0 0 1-2 0V9H7a1 1 0 0 1 0-2h4V3a1 1 0 0 1 1-1z" />
                    <circle cx="12" cy="17" r="4" opacity=".6" />
                </svg>
            </div>
            <div class="brand-name">NMB-HLabSys<span>Haematology Lab System</span></div>
        </a>

        <div class="topbar-right">
            <div class="user-chip">
                <div class="user-avatar">@(string.IsNullOrWhiteSpace(email) ? "M" : email.Substring(0, 1).ToUpper())</div>
                <span class="user-email">@email</span>
            </div>
            <a asp-controller="Home" asp-action="Logout" class="logout-btn" id="logout-btn">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                    <polyline points="16 17 21 12 16 7" />
                    <line x1="21" y1="12" x2="9" y2="12" />
                </svg>
                Sign Out
            </a>
        </div>
    </div>

    <div class="page">
        <aside class="sidebar">
            <nav aria-label="Main navigation">
                <div class="nav-section">
                    <div class="nav-label">Main</div>
                    <a asp-controller="ManagerDashboard" asp-action="Index" class="nav-item">Dashboard</a>
                    <a asp-controller="TestCatalogue" asp-action="Index" class="nav-item">Test Catalogue</a>
                    <a asp-controller="Consumable" asp-action="Index" class="nav-item">Consumables</a>
                    <a asp-controller="Staff" asp-action="Index" class="nav-item">Staff</a>
                    <a asp-controller="ConsumableOrder" asp-action="Index" class="nav-item active" aria-current="page">Orders</a>
                    <a asp-controller="ManagerDashboard" asp-action="TestsByCategory" class="nav-item">Reports</a>
                </div>
                <div class="nav-section">
                    <div class="nav-label">System</div>
                    <a asp-controller="ManagerDashboard" asp-action="Profile" class="nav-item">Profile</a>
                    <a asp-controller="ManagerDashboard" asp-action="AuditLog" class="nav-item">Audit Log</a>
                </div>
            </nav>
        </aside>

        <main class="main">
            <div class="header-row">
                <div class="heading">
                    <h1>Orders</h1>
                    <p>Create, receive and manage consumable orders.</p>
                </div>
                <button type="button" class="btn btn-primary" onclick="openModal('m-create-order')">+ Create Order</button>
            </div>

            @if (!string.IsNullOrWhiteSpace(successMessage))
            {
                <div class="alert alert-success" id="success-alert" role="status">
                    <span>✓ @successMessage</span>
                    <button type="button" class="alert-close" aria-label="Dismiss" onclick="closeAlert('success-alert')">×</button>
                </div>
            }
            @if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                <div class="alert alert-error" id="error-alert" role="alert">
                    <span>⚠ @errorMessage</span>
                    <button type="button" class="alert-close" aria-label="Dismiss" onclick="closeAlert('error-alert')">×</button>
                </div>
            }

            <!-- FILTERS: supplier, status and search -->
            <div class="filter-card">
                <form method="get" action="@Url.Action("Index", "ConsumableOrder")" class="filter-form" id="order-filter-form">
                    <div class="form-group">
                        <label class="form-label" for="supplier-filter">Supplier</label>
                        <select class="form-select" id="supplier-filter" name="supplierId">
                            <option value="">All Suppliers</option>
                            @foreach (var supplier in supplierOptions)
                            {
                                <option value="@supplier.Value" selected="@(supplier.Value == supplierFilter)">
                                    @supplier.Text
                                </option>
                            }
                        </select>
                    </div>

                    <div class="form-group">
                        <label class="form-label" for="status-filter">Order Status</label>
                        <select class="form-select" id="status-filter" name="status">
                            <option value="All Statuses" selected="@(statusFilter == "All Statuses")">All Statuses</option>
                            <option value="Ordered" selected="@(statusFilter == "Ordered")">Ordered</option>
                            <option value="Partially Complete" selected="@(statusFilter == "Partially Complete")">Partially Complete</option>
                            <option value="Complete" selected="@(statusFilter == "Complete")">Complete</option>
                            <option value="Cancelled" selected="@(statusFilter == "Cancelled")">Cancelled</option>
                        </select>
                    </div>

                    <div class="form-group">
                        <label class="form-label" for="order-search">Search</label>
                        <input type="text" id="order-search" class="form-input search-box"
                               placeholder="Search order, supplier or consumable..." autocomplete="off" />
                    </div>

                    <button type="submit" class="btn btn-primary">Apply Filters</button>
                    <a asp-controller="ConsumableOrder" asp-action="Index" class="btn btn-outline">Clear Filters</a>
                </form>
            </div>

            <div class="table-card">
                <div class="table-header">
                    <h2>Consumable Orders</h2>
                    <span class="table-count" id="table-count" aria-live="polite">@orders.Count() order(s)</span>
                </div>

                <div class="table-wrapper">
                    <table id="orders-table">
                        <thead>
                            <tr>
                                <th scope="col">Order Number</th>
                                <th scope="col">Supplier</th>
                                <th scope="col">Consumable</th>
                                <th scope="col">Order Date</th>
                                <th scope="col">Status</th>
                                <th scope="col">Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                        @if (orders.Any())
                        {
                            foreach (var order in orders)
                            {
                                var statusClass = order.Status == "Ordered" ? "status-ordered"
                                    : order.Status == "Partially Complete" ? "status-partial"
                                    : order.Status == "Complete" ? "status-complete" : "status-cancelled";

                                var supplierName = suppliers
                                    .FirstOrDefault(s => s.SupplierID == order.SupplierID)?.SupplierName;

                                if (string.IsNullOrWhiteSpace(supplierName))
                                {
                                    supplierName = supplierOptions
                                        .FirstOrDefault(s => s.Value == order.SupplierID.ToString())?.Text;
                                }

                                if (string.IsNullOrWhiteSpace(supplierName))
                                {
                                    supplierName = $"Supplier #{order.SupplierID}";
                                }

                                var consumableName = consumableItems
                                    .FirstOrDefault(c => c.Value == order.ConsumableID.ToString())?.Text
                                    ?? $"Consumable #{order.ConsumableID}";

                                <tr class="order-row"
                                    data-search="@($"{order.OrderNumber} {supplierName} {order.Items} {consumableName} {order.Status}")"
                                    data-order-id="@order.Id"
                                    data-order-number="@order.OrderNumber"
                                    data-supplier="@supplierName"
                                    data-supplier-id="@order.SupplierID"
                                    data-order-date="@order.OrderDate.ToString("dd MMM yyyy HH:mm")"
                                    data-items="@order.Items"
                                    data-status="@order.Status">

                                    <td><div class="order-number">@order.OrderNumber</div></td>
                                    <td><div class="supplier-name">@supplierName</div></td>
                                    <td><div class="items">@consumableName</div></td>
                                    <td><div class="date">@order.OrderDate.ToString("dd MMM yyyy HH:mm")</div></td>
                                    <td><span class="status @statusClass">@order.Status</span></td>
                                    <td>
                                        <div class="actions">
                                            @if (order.Status == "Ordered" || order.Status == "Partially Complete")
                                            {
                                                <button type="button" class="btn btn-success btn-small receive-order-btn"
                                                        data-order-id="@order.Id"
                                                        data-order-number="@order.OrderNumber"
                                                        data-supplier="@supplierName"
                                                        data-order-date="@order.OrderDate.ToString("dd MMM yyyy HH:mm")">
                                                    Receive
                                                </button>
                                            }
                                            @if (order.Status == "Ordered")
                                            {
                                                <button type="button" class="btn btn-danger btn-small cancel-order-btn"
                                                        data-order-id="@order.Id"
                                                        data-order-number="@order.OrderNumber"
                                                        data-supplier="@supplierName">
                                                    Cancel
                                                </button>
                                            }
                                            @if (order.Status == "Complete")
                                            {
                                                <span class="action-note action-note-complete">✓ Completed</span>
                                            }
                                            @if (order.Status == "Cancelled")
                                            {
                                                <span class="action-note action-note-cancelled">Cancelled</span>
                                            }
                                        </div>
                                    </td>
                                </tr>
                            }
                        }
                        else
                        {
                            <tr id="server-empty-row">
                                <td colspan="6">
                                    <div class="empty-state">
                                        <div class="empty-icon">📦</div>
                                        <h3>No orders found</h3>
                                        <p>There are currently no consumable orders matching your filter.</p>
                                    </div>
                                </td>
                            </tr>
                        }
                        <tr id="filter-empty-row" style="display:none;">
                            <td colspan="6">
                                <div class="empty-state">
                                    <div class="empty-icon">🔎</div>
                                    <h3>No matching orders</h3>
                                    <p>Try another supplier, status or search term.</p>
                                </div>
                            </td>
                        </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </main>
    </div>

    <form id="antiForgeryForm" style="display:none;">
        @Html.AntiForgeryToken()
    </form>

    <!-- CREATE ORDER MODAL -->
    <div class="modal-overlay" id="m-create-order">
        <div class="modal" role="dialog" aria-modal="true" aria-labelledby="create-order-title">
            <div class="modal-header">
                <h2 class="modal-title" id="create-order-title">Create Consumable Order</h2>
                <button type="button" class="modal-close" aria-label="Close" onclick="closeModal('m-create-order')">×</button>
            </div>

            <form method="post" action="@Url.Action("CreateOrder", "ConsumableOrder")" id="create-order-form">
                @Html.AntiForgeryToken()
                <div class="modal-body">
                    <p class="modal-subtitle">Create a new order for laboratory consumables.</p>

                    <div class="modal-form-group">
                        <label class="form-label" for="order-supplier">Supplier <span class="required">*</span></label>
                        <select class="form-select" name="supplierId" id="order-supplier" required>
                            <option value="">Select Supplier</option>
                            @foreach (var supplier in supplierOptions)
                            {
                                <option value="@supplier.Value">@supplier.Text</option>
                            }
                        </select>
                    </div>

                    <div class="modal-form-group">
                        <label class="form-label" for="order-number-display">Order Number</label>
                        <input type="text" id="order-number-display" class="form-input" value="Automatically generated" readonly />
                        <small class="field-hint">The order number will be generated automatically.</small>
                    </div>

                    <div class="modal-form-group">
                        <span class="form-label">Order items <span class="required">*</span></span>
                        <p class="modal-subtitle tight">
                            Choose items supplied by the selected supplier and enter the quantity needed for each.
                        </p>

                        <div class="order-items-panel">
                            <div class="order-items-heading">
                                <span>Consumable / stock</span><span>Quantity</span><span>Action</span>
                            </div>
                            <div id="order-items-container">
                                <div class="order-item-row">
                                    <div class="order-item-main">
                                        <span class="order-item-number">01</span>
                                        <div class="order-item-fields">
                                            <select name="consumableId" class="form-select consumable-select" aria-label="Consumable" required>
                                                <option value="">Select a supplier first</option>
                                            </select>
                                            <small class="order-item-hint">Select a supplier first to see its available consumables.</small>
                                        </div>
                                    </div>
                                    <div class="quantity-cell">
                                        <label>Quantity ordered</label>
                                        <div class="quantity-stepper">
                                            <button type="button" class="quantity-step-btn quantity-decrease" aria-label="Decrease quantity">−</button>
                                            <input type="number" name="QuantityOrdered" class="form-input quantity-input"
                                                   min="1" step="1" value="1" required aria-label="Quantity ordered" />
                                            <button type="button" class="quantity-step-btn quantity-increase" aria-label="Increase quantity">+</button>
                                        </div>
                                    </div>
                                    <button type="button" class="btn btn-outline remove-item-btn">Remove</button>
                                </div>
                            </div>

                            <div class="order-items-footer">
                                <button type="button" class="btn btn-outline" id="add-item-btn">＋ Add another item</button>
                                <span class="order-items-count"><strong id="order-item-count">1</strong> item row(s)</span>
                            </div>
                        </div>

                        <div class="order-supplier-note">
                            <span aria-hidden="true">ⓘ</span>
                            <span id="supplier-items-note">Select a supplier to load only the consumables associated with that supplier.</span>
                        </div>
                        <input type="hidden" name="items" id="order-items-json" />
                    </div>
                </div>

                <div class="modal-footer">
                    <button type="button" class="btn btn-outline" onclick="closeModal('m-create-order')">Cancel</button>
                    <button type="submit" class="btn btn-primary">Create Order</button>
                </div>
            </form>
        </div>
    </div>

    <!-- RECEIVE ORDER MODAL -->
    <div class="modal-overlay" id="m-receive-order">
        <div class="modal modal-large" role="dialog" aria-modal="true" aria-labelledby="receive-order-title">
            <div class="modal-header">
                <div>
                    <h2 class="modal-title" id="receive-order-title">Receive Order</h2>
                    <div class="modal-subtitle flush" id="recv-sub"></div>
                </div>
                <button type="button" class="modal-close" aria-label="Close" onclick="closeModal('m-receive-order')">×</button>
            </div>
            <div class="modal-body">
                <div class="receive-info">
                    <div class="info-box"><div class="info-label">Order</div><div class="info-value" id="recv-order-number"></div></div>
                    <div class="info-box"><div class="info-label">Supplier</div><div class="info-value" id="recv-supplier"></div></div>
                    <div class="info-box"><div class="info-label">Order Date</div><div class="info-value" id="recv-date"></div></div>
                </div>
                <p class="receive-intro">
                    Enter the quantity received for each item. Only received quantities will be added to stock.
                </p>
                <div id="recv-items-list"></div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-outline" onclick="closeModal('m-receive-order')">Cancel</button>
                <button type="button" id="recv-confirm-btn" class="btn btn-success">Confirm Receipt &amp; Update Stock</button>
            </div>
        </div>
    </div>

    <!-- CANCEL ORDER MODAL -->
    <div class="modal-overlay" id="m-cancel-order">
        <div class="modal" role="dialog" aria-modal="true" aria-labelledby="cancel-order-title">
            <div class="modal-header">
                <h2 class="modal-title" id="cancel-order-title">Cancel Order</h2>
                <button type="button" class="modal-close" aria-label="Close" onclick="closeModal('m-cancel-order')">×</button>
            </div>
            <form id="cancel-order-form" method="post" action="@Url.Action("CancelOrder", "ConsumableOrder")">
                @Html.AntiForgeryToken()
                <div class="modal-body">
                    <div class="cancel-warning">
                        You are about to cancel the following order:
                        <strong id="cancel-sub"></strong>
                    </div>
                    <input type="hidden" id="cancel-order-id" name="orderId" />
                    <div class="modal-form-group">
                        <label class="form-label" for="cancel-reason">Cancellation Reason <span class="required">*</span></label>
                        <textarea id="cancel-reason" class="form-textarea" name="cancellationReason"
                                  rows="5"
                                  placeholder="Enter the reason for cancelling this order..." required></textarea>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-outline" onclick="closeModal('m-cancel-order')">Go Back</button>
                    <button type="submit" class="btn btn-danger">Cancel Order &amp; Notify Supplier</button>
                </div>
            </form>
        </div>
    </div>

    <!-- LOGOUT MODAL -->
    <div id="logout-modal" role="dialog" aria-modal="true" aria-labelledby="logout-modal-title">
        <div class="logout-dialog">
            <h3 id="logout-modal-title">Sign out?</h3>
            <p>You'll be returned to the login screen. Any unsaved changes will be lost.</p>
            <div class="logout-actions">
                <button type="button" id="logout-cancel" class="btn btn-outline">Cancel</button>
                <a id="logout-confirm" href="@Url.Action("Logout", "Home")" class="btn btn-danger">Sign Out</a>
            </div>
        </div>
    </div>

    <script>
        document.addEventListener("DOMContentLoaded", function () {
            "use strict";

            const consumableOptions = @Html.Raw(JsonSerializer.Serialize(consumableDetails));
            const supplierConsumablesUrl = '@Url.Action("GetConsumablesBySupplier", "ConsumableOrder")';
            const getOrderItemsUrl = '@Url.Action("GetOrderItems", "ConsumableOrder")';
            const receiveOrderUrl = '@Url.Action("ReceiveOrder", "ConsumableOrder")';

            const consumablesBySupplier = {};
            let currentReceiveOrderId = 0;

            function normalize(value) {
                return (value || "").toString().trim().toLowerCase().replace(/\s+/g, " ");
            }

            function escapeHtml(value) {
                return (value ?? "").toString()
                    .replace(/&/g, "&amp;").replace(/</g, "&lt;")
                    .replace(/>/g, "&gt;").replace(/"/g, "&quot;")
                    .replace(/'/g, "&#039;");
            }

            function getAntiForgeryToken() {
                return document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]')?.value || "";
            }

            window.openModal = function (id) {
                const modal = document.getElementById(id);
                if (modal) {
                    modal.classList.add("show");
                    modal.style.display = "flex";
                }
            };

            window.closeModal = function (id) {
                const modal = document.getElementById(id);
                if (modal) {
                    modal.classList.remove("show");
                    modal.style.display = "none";
                }
            };

            window.closeAlert = function (id) {
                document.getElementById(id)?.remove();
            };

            /* Supplier-specific consumables in the create-order modal. */
            function selectedSupplierId() {
                return parseInt(document.getElementById("order-supplier")?.value || "0", 10);
            }

            async function loadConsumablesForSupplier(supplierId) {
                if (!supplierId) return [];

                if (Object.prototype.hasOwnProperty.call(consumablesBySupplier, supplierId)) {
                    return consumablesBySupplier[supplierId];
                }

                const response = await fetch(
                    supplierConsumablesUrl + "?supplierId=" + encodeURIComponent(supplierId),
                    { headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "application/json" } }
                );

                if (!response.ok) throw new Error("Unable to load consumables for this supplier.");

                const result = await response.json();
                if (!result || result.success !== true || !Array.isArray(result.data)) {
                    throw new Error(result?.message || "No consumables were returned.");
                }

                consumablesBySupplier[supplierId] = result.data.map(item => ({
                    id: item.id ?? item.ConsumableID ?? item.consumableID,
                    name: item.name ?? item.ConsumableName ?? item.consumableName,
                    quantityOnHand: item.quantityOnHand ?? item.QuantityOnHand
                }));

                return consumablesBySupplier[supplierId];
            }

            function updateOrderItemPresentation() {
                const rows = Array.from(document.querySelectorAll("#order-items-container .order-item-row"));

                rows.forEach((row, index) => {
                    const number = row.querySelector(".order-item-number");
                    const select = row.querySelector(".consumable-select");
                    const hint = row.querySelector(".order-item-hint");

                    if (number) number.textContent = String(index + 1).padStart(2, "0");

                    if (hint && select) {
                        const options = consumablesBySupplier[selectedSupplierId()] || [];
                        const item = options.find(x => String(x.id) === String(select.value));

                        if (item) {
                            const stock = item.quantityOnHand;
                            hint.textContent = "Selected: " + item.name +
                                (stock !== null && stock !== undefined ? " · Current stock: " + stock : "");
                            hint.classList.add("has-selection");
                        } else {
                            hint.textContent = selectedSupplierId()
                                ? "Choose a consumable from the selected supplier."
                                : "Select a supplier first to see its available consumables.";
                            hint.classList.remove("has-selection");
                        }
                    }
                });

                const count = document.getElementById("order-item-count");
                if (count) count.textContent = rows.length;

                const supplier = document.getElementById("order-supplier");
                const note = document.getElementById("supplier-items-note");
                if (note) {
                    note.textContent = supplier?.value
                        ? "Consumables are filtered to " + supplier.options[supplier.selectedIndex].text +
                          ". Current stock is shown after you select an item."
                        : "Select a supplier to load only the consumables associated with that supplier.";
                }
            }

            async function populateConsumableSelect(select, preserveSelection) {
                const oldValue = preserveSelection ? select.value : "";
                const supplierId = selectedSupplierId();

                select.innerHTML = '<option value="">Loading consumables...</option>';
                select.disabled = true;

                if (!supplierId) {
                    select.innerHTML = '<option value="">Select a supplier first</option>';
                    select.disabled = false;
                    updateOrderItemPresentation();
                    return;
                }

                try {
                    const items = await loadConsumablesForSupplier(supplierId);
                    select.innerHTML = '<option value="">Select a consumable</option>';

                    items.forEach(item => {
                        const option = document.createElement("option");
                        option.value = item.id;
                        option.textContent = item.name +
                            (item.quantityOnHand !== undefined && item.quantityOnHand !== null
                                ? " (In stock: " + item.quantityOnHand + ")" : "");
                        select.appendChild(option);
                    });

                    if (oldValue && items.some(x => String(x.id) === String(oldValue))) {
                        select.value = oldValue;
                    }

                    if (items.length === 0) {
                        select.innerHTML = '<option value="">No consumables linked to this supplier</option>';
                    }
                } catch (error) {
                    console.error(error);
                    select.innerHTML = '<option value="">Could not load consumables</option>';
                    const note = document.getElementById("supplier-items-note");
                    if (note) note.textContent = error.message;
                } finally {
                    select.disabled = false;
                    updateOrderItemPresentation();
                }
            }

            async function populateAllConsumableSelects(clearSelections) {
                const selects = Array.from(document.querySelectorAll("#order-items-container .consumable-select"));
                await Promise.all(selects.map(select => populateConsumableSelect(select, !clearSelections)));
                updateOrderItemPresentation();
            }

            function resetRow(row) {
                const select = row.querySelector(".consumable-select");
                const quantity = row.querySelector(".quantity-input");
                if (select) {
                    select.value = "";
                    populateConsumableSelect(select, false);
                }
                if (quantity) quantity.value = "1";
            }

            document.getElementById("order-supplier")?.addEventListener("change", function () {
                populateAllConsumableSelects(true);
            });

            document.getElementById("add-item-btn")?.addEventListener("click", function () {
                const container = document.getElementById("order-items-container");
                const firstRow = container?.querySelector(".order-item-row");
                if (!container || !firstRow) return;

                const row = firstRow.cloneNode(true);
                container.appendChild(row);
                resetRow(row);
                updateOrderItemPresentation();
                row.querySelector(".consumable-select")?.focus();
            });

            document.getElementById("order-items-container")?.addEventListener("change", function (event) {
                if (event.target.matches(".consumable-select")) updateOrderItemPresentation();

                if (event.target.matches(".quantity-input")) {
                    const input = event.target;
                    const value = parseInt(input.value || "1", 10);
                    input.value = Number.isInteger(value) && value > 0 ? value : 1;
                }
            });

            document.getElementById("order-items-container")?.addEventListener("click", function (event) {
                const button = event.target.closest(".quantity-decrease, .quantity-increase");
                if (!button) return;

                const row = button.closest(".order-item-row");
                const input = row?.querySelector(".quantity-input");
                if (!input) return;

                const value = Math.max(1, parseInt(input.value || "1", 10) || 1);
                input.value = button.classList.contains("quantity-decrease")
                    ? Math.max(1, value - 1) : value + 1;
                input.dispatchEvent(new Event("change", { bubbles: true }));
            });

            document.addEventListener("click", function (event) {
                const button = event.target.closest(".remove-item-btn");
                if (!button) return;

                const container = document.getElementById("order-items-container");
                const rows = container?.querySelectorAll(".order-item-row");
                if (!container || !rows) return;

                if (rows.length <= 1) {
                    resetRow(rows[0]);
                    return;
                }

                button.closest(".order-item-row")?.remove();
                updateOrderItemPresentation();
            });

            populateAllConsumableSelects(true);

            /* Submit the multiple order items as JSON to the existing CreateOrder action. */
            document.getElementById("create-order-form")?.addEventListener("submit", function (event) {
                event.preventDefault();

                const supplier = document.getElementById("order-supplier");
                if (!supplier?.value) {
                    alert("Please select a supplier.");
                    supplier?.focus();
                    return;
                }

                const rows = document.querySelectorAll("#order-items-container .order-item-row");
                const items = [];
                const usedIds = new Set();

                for (const row of rows) {
                    const select = row.querySelector(".consumable-select");
                    const quantityInput = row.querySelector(".quantity-input");
                    const id = parseInt(select?.value || "0", 10);
                    const quantity = parseInt(quantityInput?.value || "0", 10);
                    const name = select?.selectedOptions[0]?.text || "consumable";

                    if (!id) {
                        alert("Please select a consumable for every item row.");
                        select?.focus();
                        return;
                    }

                    if (!Number.isInteger(quantity) || quantity <= 0) {
                        alert('Please enter a valid quantity for "' + name + '".');
                        quantityInput?.focus();
                        return;
                    }

                    if (usedIds.has(id)) {
                        alert('"' + name + '" has already been added to this order.');
                        select?.focus();
                        return;
                    }

                    usedIds.add(id);
                    items.push({ ConsumableID: id, QuantityOrdered: quantity });
                }

                if (!items.length) {
                    alert("Please add at least one consumable.");
                    return;
                }

                document.getElementById("order-items-json").value = JSON.stringify(items);
                this.submit();
            });

            /* Supplier filter, status filter and search work together on visible rows. */
            const supplierFilter = document.getElementById("supplier-filter");
            const statusFilter = document.getElementById("status-filter");
            const searchInput = document.getElementById("order-search");
            const filterForm = document.getElementById("order-filter-form");

            function filterOrders() {
                const selectedSupplier = supplierFilter?.value || "";
                const selectedStatus = statusFilter?.value || "All Statuses";
                const search = normalize(searchInput?.value || "");
                const rows = document.querySelectorAll("#orders-table tbody .order-row");
                let visible = 0;

                rows.forEach(row => {
                    const supplierMatches = !selectedSupplier ||
                        row.dataset.supplierId === selectedSupplier;

                    const statusMatches = selectedStatus === "All Statuses" ||
                        normalize(row.dataset.status) === normalize(selectedStatus);

                    const searchMatches = !search ||
                        normalize(row.dataset.search || row.textContent).includes(search);

                    const show = supplierMatches && statusMatches && searchMatches;
                    row.style.display = show ? "" : "none";
                    if (show) visible++;
                });

                const count = document.getElementById("table-count");
                if (count) count.textContent = visible + " order(s)";

                const emptyRow = document.getElementById("filter-empty-row");
                if (emptyRow) emptyRow.style.display = visible === 0 ? "" : "none";
            }

            supplierFilter?.addEventListener("change", filterOrders);
            statusFilter?.addEventListener("change", filterOrders);
            searchInput?.addEventListener("input", filterOrders);

            filterForm?.addEventListener("submit", function (event) {
                // Apply these filters to the orders already loaded on this page.
                // Do not reload and lose the supplier/search selections.
                event.preventDefault();
                filterOrders();
            });

            filterOrders();

            /* Receive order: load its individual order items from the controller. */
            function createReceiveItem(item) {
                const wrapper = document.createElement("div");
                wrapper.className = "receive-item";

                const outstanding = Math.max(0, item.quantity - item.quantityReceived);

                wrapper.innerHTML = `
                    <div>
                        <div class="receive-item-name">${escapeHtml(item.name)}</div>
                        <div class="receive-item-qty">
                            Ordered: ${item.quantity} · Already received: ${item.quantityReceived} · Remaining: ${outstanding}
                        </div>
                    </div>
                    <div></div>
                    <div>
                        <label class="form-label">Quantity received</label>
                        <input type="number" class="form-input receive-quantity"
                               min="0" max="${outstanding}" step="1" value="0"
                               data-order-item-id="${item.orderItemId}" />
                    </div>`;

                if (!item.orderItemId || outstanding <= 0) {
                    wrapper.querySelector(".receive-quantity").disabled = true;
                }
                return wrapper;
            }

            document.addEventListener("click", async function (event) {
                const button = event.target.closest(".receive-order-btn");
                if (!button) return;

                const orderId = parseInt(button.dataset.orderId || "0", 10);
                if (!orderId) {
                    alert("Invalid order.");
                    return;
                }

                currentReceiveOrderId = orderId;
                document.getElementById("recv-order-number").textContent = button.dataset.orderNumber || "";
                document.getElementById("recv-supplier").textContent = button.dataset.supplier || "";
                document.getElementById("recv-date").textContent = button.dataset.orderDate || "";
                document.getElementById("recv-sub").textContent = "Receive items for order " + (button.dataset.orderNumber || "");

                const container = document.getElementById("recv-items-list");
                container.innerHTML = '<div class="receive-status">Loading consumable items...</div>';
                openModal("m-receive-order");

                try {
                    const response = await fetch(getOrderItemsUrl + "?orderId=" + encodeURIComponent(orderId), {
                        headers: { "Accept": "application/json" },
                        cache: "no-store"
                    });

                    if (!response.ok) throw new Error("Unable to load the order items.");

                    const result = await response.json();
                    const rawItems = Array.isArray(result) ? result : (result.items ?? result.Items ?? []);

                    if (currentReceiveOrderId !== orderId) return;

                    const items = rawItems.map(item => ({
                        orderItemId: parseInt(item.OrderItemID ?? item.orderItemID ?? item.orderItemId ?? "0", 10),
                        name: item.ConsumableName ?? item.consumableName ?? item.Name ?? item.name ?? "Unknown Consumable",
                        quantity: parseInt(item.QuantityOrdered ?? item.quantityOrdered ?? item.Quantity ?? item.quantity ?? "0", 10),
                        quantityReceived: parseInt(item.QuantityReceived ?? item.quantityReceived ?? "0", 10)
                    })).filter(item => item.orderItemId > 0 && item.quantity >= 0);

                    container.innerHTML = "";

                    if (!items.length) {
                        container.innerHTML = '<div class="receive-status">No consumable items were found for this order.</div>';
                    } else {
                        items.forEach(item => container.appendChild(createReceiveItem(item)));
                    }
                } catch (error) {
                    console.error(error);
                    container.innerHTML = '<div class="receive-status error">Unable to load the order items. Please close the modal and try again.</div>';
                }
            });

            document.getElementById("recv-confirm-btn")?.addEventListener("click", async function () {
                if (!currentReceiveOrderId) {
                    alert("Invalid order.");
                    return;
                }

                const inputs = document.querySelectorAll("#recv-items-list .receive-quantity");
                const receivedItems = [];

                for (const input of inputs) {
                    const quantity = parseInt(input.value || "0", 10);
                    const maxQuantity = parseInt(input.max || "0", 10);
                    const orderItemId = parseInt(input.dataset.orderItemId || "0", 10);

                    if (!Number.isInteger(quantity) || quantity < 0) {
                        alert("Please enter valid received quantities.");
                        input.focus();
                        return;
                    }

                    if (quantity > maxQuantity) {
                        alert("The received quantity cannot exceed the remaining quantity (" + maxQuantity + ").");
                        input.focus();
                        return;
                    }

                    if (quantity > 0) {
                        if (!orderItemId) {
                            alert("An order item could not be identified. Please reload the page.");
                            return;
                        }
                        receivedItems.push({ OrderItemID: orderItemId, QuantityReceived: quantity });
                    }
                }

                if (!receivedItems.length) {
                    alert("Please enter at least one received quantity.");
                    return;
                }

                const button = this;
                const originalText = button.textContent;
                button.disabled = true;
                button.textContent = "Processing...";

                try {
                    const formData = new URLSearchParams();
                    formData.append("orderId", currentReceiveOrderId.toString());
                    formData.append("receivedItemsJson", JSON.stringify(receivedItems));

                    const response = await fetch(receiveOrderUrl, {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                            "RequestVerificationToken": getAntiForgeryToken()
                        },
                        body: formData.toString()
                    });

                    if (!response.ok) throw new Error("Unable to receive order.");

                    if ((response.headers.get("content-type") || "").includes("application/json")) {
                        const result = await response.json();
                        if (result?.success === false) throw new Error(result.message || "Unable to receive order.");
                    }

                    closeModal("m-receive-order");
                    window.location.reload();
                } catch (error) {
                    alert(error.message || "An error occurred while receiving the order.");
                } finally {
                    button.disabled = false;
                    button.textContent = originalText;
                }
            });

            /* Cancel order. */
            document.addEventListener("click", function (event) {
                const button = event.target.closest(".cancel-order-btn");
                if (!button) return;

                document.getElementById("cancel-order-id").value = button.dataset.orderId || "";
                document.getElementById("cancel-reason").value = "";
                document.getElementById("cancel-sub").textContent =
                    "Order " + (button.dataset.orderNumber || "") + " — " + (button.dataset.supplier || "");

                openModal("m-cancel-order");
            });

            document.getElementById("cancel-order-form")?.addEventListener("submit", function (event) {
                const id = parseInt(document.getElementById("cancel-order-id")?.value || "0", 10);
                const reason = document.getElementById("cancel-reason");

                if (!id) {
                    event.preventDefault();
                    alert("Invalid order.");
                    return;
                }

                if (!reason?.value.trim()) {
                    event.preventDefault();
                    alert("Please enter a cancellation reason.");
                    reason?.focus();
                }
            });

            /* Logout confirmation. */
            const logoutButton = document.getElementById("logout-btn");
            const logoutModal = document.getElementById("logout-modal");

            logoutButton?.addEventListener("click", function (event) {
                event.preventDefault();
                document.getElementById("logout-confirm").href = logoutButton.href;
                logoutModal.style.display = "flex";
            });

            document.getElementById("logout-cancel")?.addEventListener("click", function () {
                logoutModal.style.display = "none";
            });

            document.addEventListener("keydown", function (event) {
                if (event.key !== "Escape") return;

                document.querySelectorAll(".modal-overlay.show").forEach(modal => {
                    modal.classList.remove("show");
                    modal.style.display = "none";
                });

                if (logoutModal) logoutModal.style.display = "none";
            });

            document.addEventListener("click", function (event) {
                if (event.target.classList.contains("modal-overlay")) {
                    event.target.classList.remove("show");
                    event.target.style.display = "none";
                }
            });
        });
    </script>
</body>
</html>
@using LabManager.Models
@using System.Text.Json
@using Microsoft.AspNetCore.Mvc.Rendering
@using Microsoft.AspNetCore.Http
@model IEnumerable<ConsumableOrder>
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

@{
    ViewData["Title"] = "Consumable Orders";
    Layout = null;

    var orders = Model ?? Enumerable.Empty<ConsumableOrder>();
    var suppliers = ViewBag.Suppliers as IEnumerable<Supplier>
                    ?? Enumerable.Empty<Supplier>();

    var supplierOptions = ViewBag.Supplier as IEnumerable<SelectListItem>
                          ?? Enumerable.Empty<SelectListItem>();

    var consumableItems = ViewBag.Consumables as IEnumerable<SelectListItem>
                          ?? ViewBag.Consumable as IEnumerable<SelectListItem>
                          ?? Enumerable.Empty<SelectListItem>();

    var consumableDetails = ViewBag.ConsumableDetails ?? Enumerable.Empty<object>();

    var statusFilter = ViewBag.StatusFilter as string
                       ?? Context.Request.Query["status"].ToString();

    if (string.IsNullOrWhiteSpace(statusFilter))
    {
        statusFilter = "All Statuses";
    }

    var supplierFilter = Context.Request.Query["supplierId"].ToString();
    var email = ViewBag.Email as string ?? "";
    var successMessage = TempData["Success"] as string;
    var errorMessage = TempData["Error"] as string;
}

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Consumable Orders — NMB-HLabSys</title>

    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=DM+Serif+Display&family=DM+Sans:wght@300;400;500;600;700&display=swap"
          rel="stylesheet" />

    <style>
        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

        :root {
            --navy: #0b1f3a;
            --navy2: #122848;
            --teal: #0d9488;
            --teal-dark: #0b7c72;
            --teal-light: rgba(13,148,136,.1);
            --white: #fff;
            --offwhite: #f4f7fb;
            --muted: #6b7a99;
            --border: #dde4f0;
            --red: #dc2626;
            --red-dark: #b91c1c;
            --red-light: rgba(220,38,38,.1);
            --green: #16a34a;
            --green-dark: #128a3e;
            --green-light: rgba(22,163,74,.1);
            --amber: #d97706;
            --amber-light: rgba(217,119,6,.1);
            --purple: #4f46e5;
            --purple-light: rgba(79,70,229,.1);
            --radius: 16px;
            --shadow: 0 2px 8px rgba(11,31,58,.04);
            --shadow-lg: 0 20px 60px rgba(11,31,58,.20);
            --topbar-h: 64px;
        }

        html { -webkit-text-size-adjust: 100%; }

        body {
            font-family: 'DM Sans', sans-serif;
            background: var(--offwhite);
            color: var(--navy);
            min-height: 100vh;
            line-height: 1.5;
            -webkit-font-smoothing: antialiased;
        }

        button, input, select, textarea { font-family: inherit; font-size: inherit; color: inherit; }
        button { cursor: pointer; }
        a { color: inherit; }

        :focus-visible { outline: 2px solid var(--teal); outline-offset: 2px; }
        .form-input:focus-visible, .form-select:focus-visible, .form-textarea:focus-visible { outline: none; }

        /* ---------- Top bar ---------- */
        .topbar {
            background: var(--navy);
            padding: 0 40px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            height: var(--topbar-h);
            position: sticky;
            top: 0;
            z-index: 100;
            box-shadow: 0 2px 20px rgba(0,0,0,.15);
        }

        .brand { display: flex; align-items: center; gap: 10px; text-decoration: none; }
        .brand-icon {
            width: 34px; height: 34px; background: var(--teal); flex-shrink: 0;
            border-radius: 8px; display: flex; align-items: center; justify-content: center;
        }
        .brand-icon svg { width: 18px; height: 18px; fill: white; }
        .brand-name { font-family: 'DM Serif Display', serif; color: white; font-size: 1.1rem; line-height: 1.1; }
        .brand-name span {
            display: block; font-family: 'DM Sans', sans-serif; font-size: .6rem;
            font-weight: 300; color: rgba(255,255,255,.55); letter-spacing: .1em; text-transform: uppercase;
        }
        .topbar-right { display: flex; align-items: center; gap: 16px; }
        .user-chip {
            display: flex; align-items: center; gap: 10px;
            background: rgba(255,255,255,.07); border: 1px solid rgba(255,255,255,.1);
            border-radius: 100px; padding: 6px 14px 6px 8px;
        }
        .user-avatar {
            width: 28px; height: 28px; background: var(--teal); border-radius: 50%; flex-shrink: 0;
            display: flex; align-items: center; justify-content: center;
            font-size: .75rem; font-weight: 600; color: white;
        }
        .user-email {
            font-size: .8rem; color: rgba(255,255,255,.8);
            max-width: 160px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
        }
        .logout-btn {
            background: rgba(255,255,255,.08); border: 1px solid rgba(255,255,255,.12);
            color: rgba(255,255,255,.85); padding: 8px 16px; border-radius: 8px;
            text-decoration: none; font-size: .82rem; font-weight: 500;
            display: flex; align-items: center; gap: 6px; cursor: pointer;
            transition: background .15s, color .15s; white-space: nowrap;
        }
        .logout-btn:hover { background: rgba(255,255,255,.15); color: white; }
        .logout-btn svg { width: 14px; height: 14px; flex-shrink: 0; }

        /* ---------- Layout ---------- */
        .page { display: flex; min-height: calc(100vh - var(--topbar-h)); }
        .sidebar {
            width: 220px; background: white; border-right: 1px solid var(--border);
            padding: 24px 0; flex-shrink: 0; position: sticky; top: var(--topbar-h);
            height: calc(100vh - var(--topbar-h)); overflow-y: auto; align-self: flex-start;
        }
        .nav-section { margin-bottom: 8px; }
        .nav-label {
            font-size: .65rem; font-weight: 600; color: var(--muted);
            letter-spacing: .1em; text-transform: uppercase; padding: 0 20px; margin-bottom: 4px;
        }
        .nav-item {
            display: flex; align-items: center; gap: 10px; padding: 10px 20px;
            color: var(--muted); text-decoration: none; font-size: .85rem;
            font-weight: 500; transition: background .15s, color .15s; border-left: 3px solid transparent;
        }
        .nav-item:hover { background: var(--offwhite); color: var(--navy); }
        .nav-item.active {
            background: var(--teal-light); color: var(--teal-dark);
            border-left-color: var(--teal); font-weight: 600;
        }
        .nav-item svg { width: 16px; height: 16px; flex-shrink: 0; }

        .main { flex: 1; padding: clamp(24px, 3vw, 40px); min-width: 0; }
        .header-row {
            display: flex; justify-content: space-between; align-items: flex-start;
            margin-bottom: 24px; gap: 20px; flex-wrap: wrap;
        }
        .heading h1 { font-family: 'DM Serif Display', serif; font-size: 1.8rem; font-weight: 400; line-height: 1.2; }
        .heading p { margin-top: 6px; color: var(--muted); font-size: .85rem; font-weight: 400; }

        /* ---------- Buttons ---------- */
        .btn {
            display: inline-flex; align-items: center; justify-content: center; gap: 6px;
            border: 1px solid transparent; border-radius: 10px; padding: 10px 16px;
            cursor: pointer; font-size: .82rem; font-weight: 600; line-height: 1.2;
            text-decoration: none; white-space: nowrap;
            transition: background .18s ease, color .18s ease, border-color .18s ease, transform .18s ease;
        }
        .btn:hover { transform: translateY(-1px); }
        .btn:disabled { opacity: .6; cursor: not-allowed; transform: none; }
        .btn-primary { background: var(--teal); color: white; box-shadow: 0 2px 8px rgba(13,148,136,.25); }
        .btn-primary:hover { background: var(--teal-dark); }
        .btn-success { background: var(--green); color: white; }
        .btn-success:hover { background: var(--green-dark); }
        .btn-danger { background: var(--red); color: white; }
        .btn-danger:hover { background: var(--red-dark); }
        .btn-outline { background: white; color: var(--navy); border-color: var(--border); }
        .btn-outline:hover { border-color: var(--teal); color: var(--teal-dark); }
        .btn-small { padding: 7px 11px; font-size: .72rem; }

        /* ---------- Alerts ---------- */
        .alert {
            border-radius: 10px; padding: 13px 16px; margin-bottom: 20px;
            font-size: .82rem; font-weight: 600; display: flex;
            align-items: center; justify-content: space-between; gap: 12px;
        }
        .alert-success { background: var(--green-light); color: var(--green-dark); border: 1px solid rgba(22,163,74,.25); }
        .alert-error { background: var(--red-light); color: var(--red-dark); border: 1px solid rgba(220,38,38,.25); }
        .alert-close { background: none; border: none; cursor: pointer; font-size: 18px; line-height: 1; color: inherit; padding: 2px 6px; }

        /* ---------- Filters ---------- */
        .filter-card {
            background: white; border: 1px solid var(--border); border-radius: var(--radius);
            padding: 18px; margin-bottom: 20px; box-shadow: var(--shadow);
        }
        .filter-form { display: flex; gap: 15px; align-items: flex-end; flex-wrap: wrap; }
        .form-group { display: flex; flex-direction: column; gap: 7px; min-width: 160px; }
        .form-label { display: block; font-size: .72rem; font-weight: 700; color: var(--navy); }
        .form-input, .form-select, .form-textarea {
            border: 1px solid var(--border); border-radius: 8px; padding: 10px 12px;
            background: white; color: var(--navy); outline: none; font-size: .82rem;
            transition: border-color .15s, box-shadow .15s;
        }
        .form-input:focus, .form-select:focus, .form-textarea:focus {
            border-color: var(--teal); box-shadow: 0 0 0 3px var(--teal-light);
        }
        .form-input[readonly] { background: var(--offwhite); color: var(--muted); }
        .form-select:disabled { background: var(--offwhite); color: var(--muted); cursor: wait; }
        .form-textarea { resize: vertical; }
        .search-box { min-width: 260px; }

        /* ---------- Table ---------- */
        .table-card {
            background: white; border: 1px solid var(--border);
            border-radius: var(--radius); overflow: hidden; box-shadow: var(--shadow);
        }
        .table-header {
            padding: 17px 22px; border-bottom: 1px solid var(--border);
            display: flex; justify-content: space-between; align-items: center; gap: 12px;
        }
        .table-header h2 { font-family: 'DM Serif Display', serif; font-size: 1.05rem; font-weight: 400; }
        .table-count {
            font-size: .72rem; font-weight: 600; color: var(--muted); background: var(--offwhite);
            border: 1px solid var(--border); padding: 5px 10px; border-radius: 100px; white-space: nowrap;
        }
        .table-wrapper { width: 100%; overflow-x: auto; -webkit-overflow-scrolling: touch; }
        table { width: 100%; border-collapse: collapse; min-width: 950px; }
        th {
            background: var(--offwhite); color: var(--muted); font-size: .7rem; font-weight: 600;
            text-transform: uppercase; letter-spacing: .08em; text-align: left;
            padding: 12px 18px; border-bottom: 1px solid var(--border); white-space: nowrap;
        }
        td {
            padding: 15px 18px; border-bottom: 1px solid var(--offwhite);
            font-size: .83rem; vertical-align: middle; color: var(--navy);
        }
        tbody tr:last-child td { border-bottom: none; }
        tbody tr:hover { background: var(--offwhite); }
        .order-number, .supplier-name { font-weight: 700; }
        .items { color: var(--muted); max-width: 320px; line-height: 1.5; overflow-wrap: anywhere; }
        .date { color: var(--muted); white-space: nowrap; }
        .status {
            display: inline-flex; align-items: center; padding: 4px 10px;
            border-radius: 100px; font-size: .7rem; font-weight: 700; white-space: nowrap;
        }
        .status-ordered { color: var(--purple); background: var(--purple-light); }
        .status-partial { color: var(--amber); background: var(--amber-light); }
        .status-complete { color: var(--green-dark); background: var(--green-light); }
        .status-cancelled { color: var(--red-dark); background: var(--red-light); }
        .actions { display: flex; gap: 7px; flex-wrap: wrap; align-items: center; }
        .action-note { font-size: .75rem; font-weight: 600; }
        .action-note-complete { color: var(--green-dark); }
        .action-note-cancelled { color: var(--red-dark); }
        .empty-state { text-align: center; padding: 60px 20px; color: var(--muted); }
        .empty-icon {
            width: 70px; height: 70px; margin: 0 auto 17px; border-radius: 50%;
            background: var(--teal-light); display: flex; align-items: center; justify-content: center; font-size: 32px;
        }
        .empty-state h3 { font-size: 1rem; font-weight: 700; color: var(--navy); margin-bottom: 5px; }
        .empty-state p { font-size: .8rem; }

        /* ---------- Modals ---------- */
        .modal-overlay {
            position: fixed; inset: 0; background: rgba(11,31,58,.45);
            -webkit-backdrop-filter: blur(3px); backdrop-filter: blur(3px);
            display: none; align-items: center;
            justify-content: center; z-index: 1000; padding: 20px;
        }
        .modal-overlay.show { display: flex; }
        .modal {
            width: 100%; max-width: 560px; background: white; border-radius: var(--radius);
            box-shadow: var(--shadow-lg); max-height: 90vh; overflow-y: auto; animation: modalIn .18s ease;
        }
        .modal-large { max-width: 700px; }
        @@keyframes modalIn {
            from { opacity: 0; transform: scale(.96) translateY(8px); }
            to { opacity: 1; transform: scale(1) translateY(0); }
        }
        .modal-header {
            padding: 19px 22px; border-bottom: 1px solid var(--border);
            display: flex; justify-content: space-between; align-items: center; gap: 12px;
        }
        .modal-title { font-family: 'DM Serif Display', serif; font-size: 1.2rem; font-weight: 400; }
        .modal-close {
            width: 32px; height: 32px; border: none; background: var(--offwhite); flex-shrink: 0;
            border-radius: 8px; font-size: 20px; line-height: 1; color: var(--muted); cursor: pointer;
            transition: background .15s, color .15s;
        }
        .modal-close:hover { background: var(--border); color: var(--navy); }
        .modal-body { padding: 22px; }
        .modal-footer {
            padding: 16px 22px; border-top: 1px solid var(--border);
            display: flex; justify-content: flex-end; gap: 10px; background: var(--offwhite);
            border-radius: 0 0 var(--radius) var(--radius); flex-wrap: wrap;
        }
        .modal-subtitle { color: var(--muted); font-size: .82rem; margin: 5px 0 20px; }
        .modal-subtitle.flush { margin: 0; }
        .modal-subtitle.tight { margin: 0 0 12px; }
        .modal-form-group { margin-bottom: 17px; }
        .modal-form-group .form-label { margin-bottom: 7px; }
        .modal-form-group .form-input,
        .modal-form-group .form-select,
        .modal-form-group .form-textarea { width: 100%; }
        .field-hint { display: block; margin-top: 5px; color: var(--muted); font-size: .72rem; }
        .required { color: var(--red); }

        .cancel-warning {
            background: var(--red-light); color: var(--red-dark); border: 1px solid rgba(220,38,38,.25);
            padding: 13px; border-radius: 10px; margin-bottom: 18px; font-size: .82rem;
        }
        .cancel-warning strong { display: block; margin-top: 5px; }

        .receive-intro { font-size: .82rem; color: var(--muted); margin-bottom: 15px; }
        .receive-info { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin-bottom: 20px; }
        .info-box { background: var(--offwhite); border: 1px solid var(--border); padding: 12px; border-radius: 10px; min-width: 0; }
        .info-label { font-size: .65rem; text-transform: uppercase; letter-spacing: .06em; color: var(--muted); margin-bottom: 4px; }
        .info-value { font-size: .83rem; font-weight: 700; overflow-wrap: anywhere; }
        .receive-item {
            border: 1px solid var(--border); border-radius: 10px; padding: 13px; margin-bottom: 10px;
            display: grid; grid-template-columns: 1fr 140px; gap: 15px; align-items: center;
        }
        .receive-item > div:empty { display: none; }
        .receive-item .receive-quantity { width: 100%; }
        .receive-item-name { font-weight: 700; font-size: .83rem; }
        .receive-item-qty { font-size: .75rem; color: var(--muted); }
        .receive-item-qty, .receive-item-name { overflow-wrap: anywhere; }
        .receive-item .form-label { margin-bottom: 6px; }
        .form-input:disabled { background: var(--offwhite); color: var(--muted); cursor: not-allowed; }
        .receive-status { padding: 15px; font-size: .82rem; }
        .receive-status.error { color: var(--red-dark); }

        /* ---------- Create-order item rows ---------- */
        .order-items-panel { border: 1px solid var(--border); border-radius: 14px; overflow: hidden; background: #fbfcff; }
        .order-items-heading {
            display: grid; grid-template-columns: minmax(0,1fr) 125px 84px; gap: 12px;
            padding: 10px 14px; background: #f0f4fa; border-bottom: 1px solid var(--border);
            color: var(--muted); font-size: .69rem; font-weight: 700; letter-spacing: .07em; text-transform: uppercase;
        }
        #order-items-container { display: grid; gap: 10px; padding: 12px; }
        .order-item-row {
            display: grid !important; grid-template-columns: minmax(0,1fr) 125px 84px;
            align-items: center !important; gap: 12px !important; margin: 0 !important;
            padding: 12px; background: white; border: 1px solid var(--border);
            border-radius: 12px; box-shadow: 0 2px 5px rgba(11,31,58,.025);
            transition: border-color .15s, box-shadow .15s;
        }
        .order-item-row:focus-within { border-color: rgba(13,148,136,.65); box-shadow: 0 0 0 3px rgba(13,148,136,.08); }
        .order-item-main { min-width: 0; display: flex; align-items: flex-start; gap: 10px; }
        .order-item-number {
            display: inline-flex; flex: 0 0 30px; width: 30px; height: 30px;
            align-items: center; justify-content: center; border-radius: 9px;
            color: var(--teal-dark); background: var(--teal-light); font-size: .78rem; font-weight: 700;
        }
        .order-item-fields { min-width: 0; flex: 1; }
        .order-item-fields .form-select { width: 100%; min-width: 0; text-overflow: ellipsis; }
        .order-item-hint { display: block; margin-top: 5px; color: var(--muted); font-size: .72rem; line-height: 1.4; }
        .order-item-hint.has-selection { color: var(--teal-dark); }
        .quantity-cell label { display: block; margin-bottom: 7px; font-size: .72rem; font-weight: 700; }
        .quantity-stepper {
            display: grid; grid-template-columns: 30px minmax(35px,1fr) 30px;
            border: 1px solid var(--border); border-radius: 10px; overflow: hidden; background: white;
        }
        .quantity-stepper:focus-within { border-color: var(--teal); box-shadow: 0 0 0 3px var(--teal-light); }
        .quantity-input {
            width: 100%; min-width: 0; text-align: center; font-weight: 700;
            border: 0 !important; border-radius: 0 !important; padding: 9px 2px !important;
            box-shadow: none !important; -moz-appearance: textfield; appearance: textfield;
        }
        .quantity-input::-webkit-outer-spin-button,
        .quantity-input::-webkit-inner-spin-button { -webkit-appearance: none; margin: 0; }
        .quantity-step-btn {
            border: 0; background: #f0f7f7; color: var(--teal-dark); font-size: 1.1rem; font-weight: 700;
            cursor: pointer; transition: background .15s;
        }
        .quantity-step-btn:hover { background: var(--teal-light); }
        .remove-item-btn { width: 100%; min-width: 0; padding-left: 8px !important; padding-right: 8px !important; }
        .order-items-footer { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 0 14px 14px; flex-wrap: wrap; }
        .order-items-count { color: var(--muted); font-size: .76rem; }
        .order-supplier-note {
            display: flex; gap: 9px; align-items: flex-start; padding: 11px 12px; margin-top: 10px;
            border-radius: 10px; background: var(--teal-light); color: var(--navy2); font-size: .77rem;
        }

        /* ---------- Logout modal ---------- */
        #logout-modal {
            display: none; position: fixed; inset: 0; z-index: 1100; padding: 20px;
            background: rgba(11,31,58,.45); align-items: center; justify-content: center;
        }
        .logout-dialog {
            background: white; border-radius: 20px; padding: 32px 28px;
            max-width: 380px; width: 100%; box-shadow: 0 20px 60px rgba(11,31,58,.2);
        }
        .logout-dialog h3 { font-family: 'DM Serif Display', serif; font-size: 1.2rem; font-weight: 400; color: var(--navy); margin-bottom: 8px; }
        .logout-dialog p { font-size: .85rem; color: var(--muted); margin-bottom: 24px; line-height: 1.6; }
        .logout-actions { display: flex; gap: 10px; }
        .logout-actions .btn { flex: 1; }

        /* ---------- Responsive ---------- */
        @@media (max-width: 768px) {
            .topbar { padding: 0 20px; }
            .user-email { display: none; }
            .user-chip { padding: 6px; }
            .sidebar { display: none; }
            .main { padding: 20px 16px; }
            .header-row { flex-direction: column; align-items: flex-start; }
            .receive-item { grid-template-columns: 1fr; }
            .receive-info { grid-template-columns: 1fr; }
            .filter-form .form-group { flex: 1 1 100%; }
            .filter-form .btn { flex: 1 1 auto; }
            .modal-body { padding: 18px; }
        }
        @@media (max-width: 600px) {
            .order-items-heading { display: none; }
            .order-item-row { grid-template-columns: minmax(0,1fr) 84px !important; gap: 10px !important; }
            .order-item-main { grid-column: 1 / -1; }
            .quantity-cell { grid-column: 1; }
            .remove-item-btn { grid-column: 2; align-self: end; }
            .order-items-footer { align-items: flex-start; flex-direction: column; }
            .search-box { min-width: 0; width: 100%; }
            .logout-actions { flex-direction: column-reverse; }
        }
        @@media (prefers-reduced-motion: reduce) {
            *, *::before, *::after { animation: none !important; transition: none !important; }
            .btn:hover { transform: none; }
        }
    </style>
</head>

<body>
    <div class="topbar">
        <a class="brand" asp-controller="Home" asp-action="Index">
            <div class="brand-icon">
                <svg viewBox="0 0 24 24" aria-hidden="true">
                    <path d="M12 2a1 1 0 0 1 1 1v4h4a1 1 0 0 1 0 2h-4v4a1 1 0 0 1-2 0V9H7a1 1 0 0 1 0-2h4V3a1 1 0 0 1 1-1z" />
                    <circle cx="12" cy="17" r="4" opacity=".6" />
                </svg>
            </div>
            <div class="brand-name">NMB-HLabSys<span>Haematology Lab System</span></div>
        </a>

        <div class="topbar-right">
            <div class="user-chip">
                <div class="user-avatar">@(string.IsNullOrWhiteSpace(email) ? "M" : email.Substring(0, 1).ToUpper())</div>
                <span class="user-email">@email</span>
            </div>
            <a asp-controller="Home" asp-action="Logout" class="logout-btn" id="logout-btn">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                    <polyline points="16 17 21 12 16 7" />
                    <line x1="21" y1="12" x2="9" y2="12" />
                </svg>
                Sign Out
            </a>
        </div>
    </div>

    <div class="page">
        <aside class="sidebar">
            <nav aria-label="Main navigation">
                <div class="nav-section">
                    <div class="nav-label">Main</div>
                    <a asp-controller="ManagerDashboard" asp-action="Index" class="nav-item">Dashboard</a>
                    <a asp-controller="TestCatalogue" asp-action="Index" class="nav-item">Test Catalogue</a>
                    <a asp-controller="Consumable" asp-action="Index" class="nav-item">Consumables</a>
                    <a asp-controller="Staff" asp-action="Index" class="nav-item">Staff</a>
                    <a asp-controller="ConsumableOrder" asp-action="Index" class="nav-item active" aria-current="page">Orders</a>
                    <a asp-controller="ManagerDashboard" asp-action="TestsByCategory" class="nav-item">Reports</a>
                </div>
                <div class="nav-section">
                    <div class="nav-label">System</div>
                    <a asp-controller="ManagerDashboard" asp-action="Profile" class="nav-item">Profile</a>
                    <a asp-controller="ManagerDashboard" asp-action="AuditLog" class="nav-item">Audit Log</a>
                </div>
            </nav>
        </aside>

        <main class="main">
            <div class="header-row">
                <div class="heading">
                    <h1>Orders</h1>
                    <p>Create, receive and manage consumable orders.</p>
                </div>
                <button type="button" class="btn btn-primary" onclick="openModal('m-create-order')">+ Create Order</button>
            </div>

            @if (!string.IsNullOrWhiteSpace(successMessage))
            {
                <div class="alert alert-success" id="success-alert" role="status">
                    <span>✓ @successMessage</span>
                    <button type="button" class="alert-close" aria-label="Dismiss" onclick="closeAlert('success-alert')">×</button>
                </div>
            }
            @if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                <div class="alert alert-error" id="error-alert" role="alert">
                    <span>⚠ @errorMessage</span>
                    <button type="button" class="alert-close" aria-label="Dismiss" onclick="closeAlert('error-alert')">×</button>
                </div>
            }

            <!-- FILTERS: supplier, status and search -->
            <div class="filter-card">
                <form method="get" action="@Url.Action("Index", "ConsumableOrder")" class="filter-form" id="order-filter-form">
                    <div class="form-group">
                        <label class="form-label" for="supplier-filter">Supplier</label>
                        <select class="form-select" id="supplier-filter" name="supplierId">
                            <option value="">All Suppliers</option>
                            @foreach (var supplier in supplierOptions)
                            {
                                <option value="@supplier.Value" selected="@(supplier.Value == supplierFilter)">
                                    @supplier.Text
                                </option>
                            }
                        </select>
                    </div>

                    <div class="form-group">
                        <label class="form-label" for="status-filter">Order Status</label>
                        <select class="form-select" id="status-filter" name="status">
                            <option value="All Statuses" selected="@(statusFilter == "All Statuses")">All Statuses</option>
                            <option value="Ordered" selected="@(statusFilter == "Ordered")">Ordered</option>
                            <option value="Partially Complete" selected="@(statusFilter == "Partially Complete")">Partially Complete</option>
                            <option value="Complete" selected="@(statusFilter == "Complete")">Complete</option>
                            <option value="Cancelled" selected="@(statusFilter == "Cancelled")">Cancelled</option>
                        </select>
                    </div>

                    <div class="form-group">
                        <label class="form-label" for="order-search">Search</label>
                        <input type="text" id="order-search" class="form-input search-box"
                               placeholder="Search order, supplier or consumable..." autocomplete="off" />
                    </div>

                    <button type="submit" class="btn btn-primary">Apply Filters</button>
                    <a asp-controller="ConsumableOrder" asp-action="Index" class="btn btn-outline">Clear Filters</a>
                </form>
            </div>

            <div class="table-card">
                <div class="table-header">
                    <h2>Consumable Orders</h2>
                    <span class="table-count" id="table-count" aria-live="polite">@orders.Count() order(s)</span>
                </div>

                <div class="table-wrapper">
                    <table id="orders-table">
                        <thead>
                            <tr>
                                <th scope="col">Order Number</th>
                                <th scope="col">Supplier</th>
                                <th scope="col">Consumable</th>
                                <th scope="col">Order Date</th>
                                <th scope="col">Status</th>
                                <th scope="col">Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                        @if (orders.Any())
                        {
                            foreach (var order in orders)
                            {
                                var statusClass = order.Status == "Ordered" ? "status-ordered"
                                    : order.Status == "Partially Complete" ? "status-partial"
                                    : order.Status == "Complete" ? "status-complete" : "status-cancelled";

                                var supplierName = suppliers
                                    .FirstOrDefault(s => s.SupplierID == order.SupplierID)?.SupplierName;

                                if (string.IsNullOrWhiteSpace(supplierName))
                                {
                                    supplierName = supplierOptions
                                        .FirstOrDefault(s => s.Value == order.SupplierID.ToString())?.Text;
                                }

                                if (string.IsNullOrWhiteSpace(supplierName))
                                {
                                    supplierName = $"Supplier #{order.SupplierID}";
                                }

                                var consumableName = consumableItems
                                    .FirstOrDefault(c => c.Value == order.ConsumableID.ToString())?.Text
                                    ?? $"Consumable #{order.ConsumableID}";

                                <tr class="order-row"
                                    data-search="@($"{order.OrderNumber} {supplierName} {order.Items} {consumableName} {order.Status}")"
                                    data-order-id="@order.Id"
                                    data-order-number="@order.OrderNumber"
                                    data-supplier="@supplierName"
                                    data-supplier-id="@order.SupplierID"
                                    data-order-date="@order.OrderDate.ToString("dd MMM yyyy HH:mm")"
                                    data-items="@order.Items"
                                    data-status="@order.Status">

                                    <td><div class="order-number">@order.OrderNumber</div></td>
                                    <td><div class="supplier-name">@supplierName</div></td>
                                    <td><div class="items">@consumableName</div></td>
                                    <td><div class="date">@order.OrderDate.ToString("dd MMM yyyy HH:mm")</div></td>
                                    <td><span class="status @statusClass">@order.Status</span></td>
                                    <td>
                                        <div class="actions">
                                            @if (order.Status == "Ordered" || order.Status == "Partially Complete")
                                            {
                                                <button type="button" class="btn btn-success btn-small receive-order-btn"
                                                        data-order-id="@order.Id"
                                                        data-order-number="@order.OrderNumber"
                                                        data-supplier="@supplierName"
                                                        data-order-date="@order.OrderDate.ToString("dd MMM yyyy HH:mm")">
                                                    Receive
                                                </button>
                                            }
                                            @if (order.Status == "Ordered")
                                            {
                                                <button type="button" class="btn btn-danger btn-small cancel-order-btn"
                                                        data-order-id="@order.Id"
                                                        data-order-number="@order.OrderNumber"
                                                        data-supplier="@supplierName">
                                                    Cancel
                                                </button>
                                            }
                                            @if (order.Status == "Complete")
                                            {
                                                <span class="action-note action-note-complete">✓ Completed</span>
                                            }
                                            @if (order.Status == "Cancelled")
                                            {
                                                <span class="action-note action-note-cancelled">Cancelled</span>
                                            }
                                        </div>
                                    </td>
                                </tr>
                            }
                        }
                        else
                        {
                            <tr id="server-empty-row">
                                <td colspan="6">
                                    <div class="empty-state">
                                        <div class="empty-icon">📦</div>
                                        <h3>No orders found</h3>
                                        <p>There are currently no consumable orders matching your filter.</p>
                                    </div>
                                </td>
                            </tr>
                        }
                        <tr id="filter-empty-row" style="display:none;">
                            <td colspan="6">
                                <div class="empty-state">
                                    <div class="empty-icon">🔎</div>
                                    <h3>No matching orders</h3>
                                    <p>Try another supplier, status or search term.</p>
                                </div>
                            </td>
                        </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </main>
    </div>

    <form id="antiForgeryForm" style="display:none;">
        @Html.AntiForgeryToken()
    </form>

    <!-- CREATE ORDER MODAL -->
    <div class="modal-overlay" id="m-create-order">
        <div class="modal" role="dialog" aria-modal="true" aria-labelledby="create-order-title">
            <div class="modal-header">
                <h2 class="modal-title" id="create-order-title">Create Consumable Order</h2>
                <button type="button" class="modal-close" aria-label="Close" onclick="closeModal('m-create-order')">×</button>
            </div>

            <form method="post" action="@Url.Action("CreateOrder", "ConsumableOrder")" id="create-order-form">
                @Html.AntiForgeryToken()
                <div class="modal-body">
                    <p class="modal-subtitle">Create a new order for laboratory consumables.</p>

                    <div class="modal-form-group">
                        <label class="form-label" for="order-supplier">Supplier <span class="required">*</span></label>
                        <select class="form-select" name="supplierId" id="order-supplier" required>
                            <option value="">Select Supplier</option>
                            @foreach (var supplier in supplierOptions)
                            {
                                <option value="@supplier.Value">@supplier.Text</option>
                            }
                        </select>
                    </div>

                    <div class="modal-form-group">
                        <label class="form-label" for="order-number-display">Order Number</label>
                        <input type="text" id="order-number-display" class="form-input" value="Automatically generated" readonly />
                        <small class="field-hint">The order number will be generated automatically.</small>
                    </div>

                    <div class="modal-form-group">
                        <span class="form-label">Order items <span class="required">*</span></span>
                        <p class="modal-subtitle tight">
                            Choose items supplied by the selected supplier and enter the quantity needed for each.
                        </p>

                        <div class="order-items-panel">
                            <div class="order-items-heading">
                                <span>Consumable / stock</span><span>Quantity</span><span>Action</span>
                            </div>
                            <div id="order-items-container">
                                <div class="order-item-row">
                                    <div class="order-item-main">
                                        <span class="order-item-number">01</span>
                                        <div class="order-item-fields">
                                            <select name="consumableId" class="form-select consumable-select" aria-label="Consumable" required>
                                                <option value="">Select a supplier first</option>
                                            </select>
                                            <small class="order-item-hint">Select a supplier first to see its available consumables.</small>
                                        </div>
                                    </div>
                                    <div class="quantity-cell">
                                        <label>Quantity ordered</label>
                                        <div class="quantity-stepper">
                                            <button type="button" class="quantity-step-btn quantity-decrease" aria-label="Decrease quantity">−</button>
                                            <input type="number" name="QuantityOrdered" class="form-input quantity-input"
                                                   min="1" step="1" value="1" required aria-label="Quantity ordered" />
                                            <button type="button" class="quantity-step-btn quantity-increase" aria-label="Increase quantity">+</button>
                                        </div>
                                    </div>
                                    <button type="button" class="btn btn-outline remove-item-btn">Remove</button>
                                </div>
                            </div>

                            <div class="order-items-footer">
                                <button type="button" class="btn btn-outline" id="add-item-btn">＋ Add another item</button>
                                <span class="order-items-count"><strong id="order-item-count">1</strong> item row(s)</span>
                            </div>
                        </div>

                        <div class="order-supplier-note">
                            <span aria-hidden="true">ⓘ</span>
                            <span id="supplier-items-note">Select a supplier to load only the consumables associated with that supplier.</span>
                        </div>
                        <input type="hidden" name="items" id="order-items-json" />
                    </div>
                </div>

                <div class="modal-footer">
                    <button type="button" class="btn btn-outline" onclick="closeModal('m-create-order')">Cancel</button>
                    <button type="submit" class="btn btn-primary">Create Order</button>
                </div>
            </form>
        </div>
    </div>

    <!-- RECEIVE ORDER MODAL -->
    <div class="modal-overlay" id="m-receive-order">
        <div class="modal modal-large" role="dialog" aria-modal="true" aria-labelledby="receive-order-title">
            <div class="modal-header">
                <div>
                    <h2 class="modal-title" id="receive-order-title">Receive Order</h2>
                    <div class="modal-subtitle flush" id="recv-sub"></div>
                </div>
                <button type="button" class="modal-close" aria-label="Close" onclick="closeModal('m-receive-order')">×</button>
            </div>
            <div class="modal-body">
                <div class="receive-info">
                    <div class="info-box"><div class="info-label">Order</div><div class="info-value" id="recv-order-number"></div></div>
                    <div class="info-box"><div class="info-label">Supplier</div><div class="info-value" id="recv-supplier"></div></div>
                    <div class="info-box"><div class="info-label">Order Date</div><div class="info-value" id="recv-date"></div></div>
                </div>
                <p class="receive-intro">
                    Enter the quantity received for each item. Only received quantities will be added to stock.
                </p>
                <div id="recv-items-list"></div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-outline" onclick="closeModal('m-receive-order')">Cancel</button>
                <button type="button" id="recv-confirm-btn" class="btn btn-success">Confirm Receipt &amp; Update Stock</button>
            </div>
        </div>
    </div>

    <!-- CANCEL ORDER MODAL -->
    <div class="modal-overlay" id="m-cancel-order">
        <div class="modal" role="dialog" aria-modal="true" aria-labelledby="cancel-order-title">
            <div class="modal-header">
                <h2 class="modal-title" id="cancel-order-title">Cancel Order</h2>
                <button type="button" class="modal-close" aria-label="Close" onclick="closeModal('m-cancel-order')">×</button>
            </div>
            <form id="cancel-order-form" method="post" action="@Url.Action("CancelOrder", "ConsumableOrder")">
                @Html.AntiForgeryToken()
                <div class="modal-body">
                    <div class="cancel-warning">
                        You are about to cancel the following order:
                        <strong id="cancel-sub"></strong>
                    </div>
                    <input type="hidden" id="cancel-order-id" name="orderId" />
                    <div class="modal-form-group">
                        <label class="form-label" for="cancel-reason">Cancellation Reason <span class="required">*</span></label>
                        <textarea id="cancel-reason" class="form-textarea" name="cancellationReason"
                                  rows="5"
                                  placeholder="Enter the reason for cancelling this order..." required></textarea>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-outline" onclick="closeModal('m-cancel-order')">Go Back</button>
                    <button type="submit" class="btn btn-danger">Cancel Order &amp; Notify Supplier</button>
                </div>
            </form>
        </div>
    </div>

    <!-- LOGOUT MODAL -->
    <div id="logout-modal" role="dialog" aria-modal="true" aria-labelledby="logout-modal-title">
        <div class="logout-dialog">
            <h3 id="logout-modal-title">Sign out?</h3>
            <p>You'll be returned to the login screen. Any unsaved changes will be lost.</p>
            <div class="logout-actions">
                <button type="button" id="logout-cancel" class="btn btn-outline">Cancel</button>
                <a id="logout-confirm" href="@Url.Action("Logout", "Home")" class="btn btn-danger">Sign Out</a>
            </div>
        </div>
    </div>

    <script>
        document.addEventListener("DOMContentLoaded", function () {
            "use strict";

            const consumableOptions = @Html.Raw(JsonSerializer.Serialize(consumableDetails));
            const supplierConsumablesUrl = '@Url.Action("GetConsumablesBySupplier", "ConsumableOrder")';
            const getOrderItemsUrl = '@Url.Action("GetOrderItems", "ConsumableOrder")';
            const receiveOrderUrl = '@Url.Action("ReceiveOrder", "ConsumableOrder")';

            const consumablesBySupplier = {};
            let currentReceiveOrderId = 0;

            function normalize(value) {
                return (value || "").toString().trim().toLowerCase().replace(/\s+/g, " ");
            }

            function escapeHtml(value) {
                return (value ?? "").toString()
                    .replace(/&/g, "&amp;").replace(/</g, "&lt;")
                    .replace(/>/g, "&gt;").replace(/"/g, "&quot;")
                    .replace(/'/g, "&#039;");
            }

            function getAntiForgeryToken() {
                return document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]')?.value || "";
            }

            window.openModal = function (id) {
                const modal = document.getElementById(id);
                if (modal) {
                    modal.classList.add("show");
                    modal.style.display = "flex";
                }
            };

            window.closeModal = function (id) {
                const modal = document.getElementById(id);
                if (modal) {
                    modal.classList.remove("show");
                    modal.style.display = "none";
                }
            };

            window.closeAlert = function (id) {
                document.getElementById(id)?.remove();
            };

            /* Supplier-specific consumables in the create-order modal. */
            function selectedSupplierId() {
                return parseInt(document.getElementById("order-supplier")?.value || "0", 10);
            }

            async function loadConsumablesForSupplier(supplierId) {
                if (!supplierId) return [];

                if (Object.prototype.hasOwnProperty.call(consumablesBySupplier, supplierId)) {
                    return consumablesBySupplier[supplierId];
                }

                const response = await fetch(
                    supplierConsumablesUrl + "?supplierId=" + encodeURIComponent(supplierId),
                    { headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "application/json" } }
                );

                if (!response.ok) throw new Error("Unable to load consumables for this supplier.");

                const result = await response.json();
                if (!result || result.success !== true || !Array.isArray(result.data)) {
                    throw new Error(result?.message || "No consumables were returned.");
                }

                consumablesBySupplier[supplierId] = result.data.map(item => ({
                    id: item.id ?? item.ConsumableID ?? item.consumableID,
                    name: item.name ?? item.ConsumableName ?? item.consumableName,
                    quantityOnHand: item.quantityOnHand ?? item.QuantityOnHand
                }));

                return consumablesBySupplier[supplierId];
            }

            function updateOrderItemPresentation() {
                const rows = Array.from(document.querySelectorAll("#order-items-container .order-item-row"));

                rows.forEach((row, index) => {
                    const number = row.querySelector(".order-item-number");
                    const select = row.querySelector(".consumable-select");
                    const hint = row.querySelector(".order-item-hint");

                    if (number) number.textContent = String(index + 1).padStart(2, "0");

                    if (hint && select) {
                        const options = consumablesBySupplier[selectedSupplierId()] || [];
                        const item = options.find(x => String(x.id) === String(select.value));

                        if (item) {
                            const stock = item.quantityOnHand;
                            hint.textContent = "Selected: " + item.name +
                                (stock !== null && stock !== undefined ? " · Current stock: " + stock : "");
                            hint.classList.add("has-selection");
                        } else {
                            hint.textContent = selectedSupplierId()
                                ? "Choose a consumable from the selected supplier."
                                : "Select a supplier first to see its available consumables.";
                            hint.classList.remove("has-selection");
                        }
                    }
                });

                const count = document.getElementById("order-item-count");
                if (count) count.textContent = rows.length;

                const supplier = document.getElementById("order-supplier");
                const note = document.getElementById("supplier-items-note");
                if (note) {
                    note.textContent = supplier?.value
                        ? "Consumables are filtered to " + supplier.options[supplier.selectedIndex].text +
                          ". Current stock is shown after you select an item."
                        : "Select a supplier to load only the consumables associated with that supplier.";
                }
            }

            async function populateConsumableSelect(select, preserveSelection) {
                const oldValue = preserveSelection ? select.value : "";
                const supplierId = selectedSupplierId();

                select.innerHTML = '<option value="">Loading consumables...</option>';
                select.disabled = true;

                if (!supplierId) {
                    select.innerHTML = '<option value="">Select a supplier first</option>';
                    select.disabled = false;
                    updateOrderItemPresentation();
                    return;
                }

                try {
                    const items = await loadConsumablesForSupplier(supplierId);
                    select.innerHTML = '<option value="">Select a consumable</option>';

                    items.forEach(item => {
                        const option = document.createElement("option");
                        option.value = item.id;
                        option.textContent = item.name +
                            (item.quantityOnHand !== undefined && item.quantityOnHand !== null
                                ? " (In stock: " + item.quantityOnHand + ")" : "");
                        select.appendChild(option);
                    });

                    if (oldValue && items.some(x => String(x.id) === String(oldValue))) {
                        select.value = oldValue;
                    }

                    if (items.length === 0) {
                        select.innerHTML = '<option value="">No consumables linked to this supplier</option>';
                    }
                } catch (error) {
                    console.error(error);
                    select.innerHTML = '<option value="">Could not load consumables</option>';
                    const note = document.getElementById("supplier-items-note");
                    if (note) note.textContent = error.message;
                } finally {
                    select.disabled = false;
                    updateOrderItemPresentation();
                }
            }

            async function populateAllConsumableSelects(clearSelections) {
                const selects = Array.from(document.querySelectorAll("#order-items-container .consumable-select"));
                await Promise.all(selects.map(select => populateConsumableSelect(select, !clearSelections)));
                updateOrderItemPresentation();
            }

            function resetRow(row) {
                const select = row.querySelector(".consumable-select");
                const quantity = row.querySelector(".quantity-input");
                if (select) {
                    select.value = "";
                    populateConsumableSelect(select, false);
                }
                if (quantity) quantity.value = "1";
            }

            document.getElementById("order-supplier")?.addEventListener("change", function () {
                populateAllConsumableSelects(true);
            });

            document.getElementById("add-item-btn")?.addEventListener("click", function () {
                const container = document.getElementById("order-items-container");
                const firstRow = container?.querySelector(".order-item-row");
                if (!container || !firstRow) return;

                const row = firstRow.cloneNode(true);
                container.appendChild(row);
                resetRow(row);
                updateOrderItemPresentation();
                row.querySelector(".consumable-select")?.focus();
            });

            document.getElementById("order-items-container")?.addEventListener("change", function (event) {
                if (event.target.matches(".consumable-select")) updateOrderItemPresentation();

                if (event.target.matches(".quantity-input")) {
                    const input = event.target;
                    const value = parseInt(input.value || "1", 10);
                    input.value = Number.isInteger(value) && value > 0 ? value : 1;
                }
            });

            document.getElementById("order-items-container")?.addEventListener("click", function (event) {
                const button = event.target.closest(".quantity-decrease, .quantity-increase");
                if (!button) return;

                const row = button.closest(".order-item-row");
                const input = row?.querySelector(".quantity-input");
                if (!input) return;

                const value = Math.max(1, parseInt(input.value || "1", 10) || 1);
                input.value = button.classList.contains("quantity-decrease")
                    ? Math.max(1, value - 1) : value + 1;
                input.dispatchEvent(new Event("change", { bubbles: true }));
            });

            document.addEventListener("click", function (event) {
                const button = event.target.closest(".remove-item-btn");
                if (!button) return;

                const container = document.getElementById("order-items-container");
                const rows = container?.querySelectorAll(".order-item-row");
                if (!container || !rows) return;

                if (rows.length <= 1) {
                    resetRow(rows[0]);
                    return;
                }

                button.closest(".order-item-row")?.remove();
                updateOrderItemPresentation();
            });

            populateAllConsumableSelects(true);

            /* Submit the multiple order items as JSON to the existing CreateOrder action. */
            document.getElementById("create-order-form")?.addEventListener("submit", function (event) {
                event.preventDefault();

                const supplier = document.getElementById("order-supplier");
                if (!supplier?.value) {
                    alert("Please select a supplier.");
                    supplier?.focus();
                    return;
                }

                const rows = document.querySelectorAll("#order-items-container .order-item-row");
                const items = [];
                const usedIds = new Set();

                for (const row of rows) {
                    const select = row.querySelector(".consumable-select");
                    const quantityInput = row.querySelector(".quantity-input");
                    const id = parseInt(select?.value || "0", 10);
                    const quantity = parseInt(quantityInput?.value || "0", 10);
                    const name = select?.selectedOptions[0]?.text || "consumable";

                    if (!id) {
                        alert("Please select a consumable for every item row.");
                        select?.focus();
                        return;
                    }

                    if (!Number.isInteger(quantity) || quantity <= 0) {
                        alert('Please enter a valid quantity for "' + name + '".');
                        quantityInput?.focus();
                        return;
                    }

                    if (usedIds.has(id)) {
                        alert('"' + name + '" has already been added to this order.');
                        select?.focus();
                        return;
                    }

                    usedIds.add(id);
                    items.push({ ConsumableID: id, QuantityOrdered: quantity });
                }

                if (!items.length) {
                    alert("Please add at least one consumable.");
                    return;
                }

                document.getElementById("order-items-json").value = JSON.stringify(items);
                this.submit();
            });

            /* Supplier filter, status filter and search work together on visible rows. */
            const supplierFilter = document.getElementById("supplier-filter");
            const statusFilter = document.getElementById("status-filter");
            const searchInput = document.getElementById("order-search");
            const filterForm = document.getElementById("order-filter-form");

            function filterOrders() {
                const selectedSupplier = supplierFilter?.value || "";
                const selectedStatus = statusFilter?.value || "All Statuses";
                const search = normalize(searchInput?.value || "");
                const rows = document.querySelectorAll("#orders-table tbody .order-row");
                let visible = 0;

                rows.forEach(row => {
                    const supplierMatches = !selectedSupplier ||
                        row.dataset.supplierId === selectedSupplier;

                    const statusMatches = selectedStatus === "All Statuses" ||
                        normalize(row.dataset.status) === normalize(selectedStatus);

                    const searchMatches = !search ||
                        normalize(row.dataset.search || row.textContent).includes(search);

                    const show = supplierMatches && statusMatches && searchMatches;
                    row.style.display = show ? "" : "none";
                    if (show) visible++;
                });

                const count = document.getElementById("table-count");
                if (count) count.textContent = visible + " order(s)";

                const emptyRow = document.getElementById("filter-empty-row");
                if (emptyRow) emptyRow.style.display = visible === 0 ? "" : "none";
            }

            supplierFilter?.addEventListener("change", filterOrders);
            statusFilter?.addEventListener("change", filterOrders);
            searchInput?.addEventListener("input", filterOrders);

            filterForm?.addEventListener("submit", function (event) {
                // Apply these filters to the orders already loaded on this page.
                // Do not reload and lose the supplier/search selections.
                event.preventDefault();
                filterOrders();
            });

            filterOrders();

            /* Receive order: load its individual order items from the controller. */
            function createReceiveItem(item) {
                const wrapper = document.createElement("div");
                wrapper.className = "receive-item";

                const outstanding = Math.max(0, item.quantity - item.quantityReceived);

                wrapper.innerHTML = `
                    <div>
                        <div class="receive-item-name">${escapeHtml(item.name)}</div>
                        <div class="receive-item-qty">
                            Ordered: ${item.quantity} · Already received: ${item.quantityReceived} · Remaining: ${outstanding}
                        </div>
                    </div>
                    <div></div>
                    <div>
                        <label class="form-label">Quantity received</label>
                        <input type="number" class="form-input receive-quantity"
                               min="0" max="${outstanding}" step="1" value="0"
                               data-order-item-id="${item.orderItemId}" />
                    </div>`;

                if (!item.orderItemId || outstanding <= 0) {
                    wrapper.querySelector(".receive-quantity").disabled = true;
                }
                return wrapper;
            }

            document.addEventListener("click", async function (event) {
                const button = event.target.closest(".receive-order-btn");
                if (!button) return;

                const orderId = parseInt(button.dataset.orderId || "0", 10);
                if (!orderId) {
                    alert("Invalid order.");
                    return;
                }

                currentReceiveOrderId = orderId;
                document.getElementById("recv-order-number").textContent = button.dataset.orderNumber || "";
                document.getElementById("recv-supplier").textContent = button.dataset.supplier || "";
                document.getElementById("recv-date").textContent = button.dataset.orderDate || "";
                document.getElementById("recv-sub").textContent = "Receive items for order " + (button.dataset.orderNumber || "");

                const container = document.getElementById("recv-items-list");
                container.innerHTML = '<div class="receive-status">Loading consumable items...</div>';
                openModal("m-receive-order");

                try {
                    const response = await fetch(getOrderItemsUrl + "?orderId=" + encodeURIComponent(orderId), {
                        headers: { "Accept": "application/json" },
                        cache: "no-store"
                    });

                    if (!response.ok) throw new Error("Unable to load the order items.");

                    const result = await response.json();
                    const rawItems = Array.isArray(result) ? result : (result.items ?? result.Items ?? []);

                    if (currentReceiveOrderId !== orderId) return;

                    const items = rawItems.map(item => ({
                        orderItemId: parseInt(item.OrderItemID ?? item.orderItemID ?? item.orderItemId ?? "0", 10),
                        name: item.ConsumableName ?? item.consumableName ?? item.Name ?? item.name ?? "Unknown Consumable",
                        quantity: parseInt(item.QuantityOrdered ?? item.quantityOrdered ?? item.Quantity ?? item.quantity ?? "0", 10),
                        quantityReceived: parseInt(item.QuantityReceived ?? item.quantityReceived ?? "0", 10)
                    })).filter(item => item.orderItemId > 0 && item.quantity >= 0);

                    container.innerHTML = "";

                    if (!items.length) {
                        container.innerHTML = '<div class="receive-status">No consumable items were found for this order.</div>';
                    } else {
                        items.forEach(item => container.appendChild(createReceiveItem(item)));
                    }
                } catch (error) {
                    console.error(error);
                    container.innerHTML = '<div class="receive-status error">Unable to load the order items. Please close the modal and try again.</div>';
                }
            });

            document.getElementById("recv-confirm-btn")?.addEventListener("click", async function () {
                if (!currentReceiveOrderId) {
                    alert("Invalid order.");
                    return;
                }

                const inputs = document.querySelectorAll("#recv-items-list .receive-quantity");
                const receivedItems = [];

                for (const input of inputs) {
                    const quantity = parseInt(input.value || "0", 10);
                    const maxQuantity = parseInt(input.max || "0", 10);
                    const orderItemId = parseInt(input.dataset.orderItemId || "0", 10);

                    if (!Number.isInteger(quantity) || quantity < 0) {
                        alert("Please enter valid received quantities.");
                        input.focus();
                        return;
                    }

                    if (quantity > maxQuantity) {
                        alert("The received quantity cannot exceed the remaining quantity (" + maxQuantity + ").");
                        input.focus();
                        return;
                    }

                    if (quantity > 0) {
                        if (!orderItemId) {
                            alert("An order item could not be identified. Please reload the page.");
                            return;
                        }
                        receivedItems.push({ OrderItemID: orderItemId, QuantityReceived: quantity });
                    }
                }

                if (!receivedItems.length) {
                    alert("Please enter at least one received quantity.");
                    return;
                }

                const button = this;
                const originalText = button.textContent;
                button.disabled = true;
                button.textContent = "Processing...";

                try {
                    const formData = new URLSearchParams();
                    formData.append("orderId", currentReceiveOrderId.toString());
                    formData.append("receivedItemsJson", JSON.stringify(receivedItems));

                    const response = await fetch(receiveOrderUrl, {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                            "RequestVerificationToken": getAntiForgeryToken()
                        },
                        body: formData.toString()
                    });

                    if (!response.ok) throw new Error("Unable to receive order.");

                    if ((response.headers.get("content-type") || "").includes("application/json")) {
                        const result = await response.json();
                        if (result?.success === false) throw new Error(result.message || "Unable to receive order.");
                    }

                    closeModal("m-receive-order");
                    window.location.reload();
                } catch (error) {
                    alert(error.message || "An error occurred while receiving the order.");
                } finally {
                    button.disabled = false;
                    button.textContent = originalText;
                }
            });

            /* Cancel order. */
            document.addEventListener("click", function (event) {
                const button = event.target.closest(".cancel-order-btn");
                if (!button) return;

                document.getElementById("cancel-order-id").value = button.dataset.orderId || "";
                document.getElementById("cancel-reason").value = "";
                document.getElementById("cancel-sub").textContent =
                    "Order " + (button.dataset.orderNumber || "") + " — " + (button.dataset.supplier || "");

                openModal("m-cancel-order");
            });

            document.getElementById("cancel-order-form")?.addEventListener("submit", function (event) {
                const id = parseInt(document.getElementById("cancel-order-id")?.value || "0", 10);
                const reason = document.getElementById("cancel-reason");

                if (!id) {
                    event.preventDefault();
                    alert("Invalid order.");
                    return;
                }

                if (!reason?.value.trim()) {
                    event.preventDefault();
                    alert("Please enter a cancellation reason.");
                    reason?.focus();
                }
            });

            /* Logout confirmation. */
            const logoutButton = document.getElementById("logout-btn");
            const logoutModal = document.getElementById("logout-modal");

            logoutButton?.addEventListener("click", function (event) {
                event.preventDefault();
                document.getElementById("logout-confirm").href = logoutButton.href;
                logoutModal.style.display = "flex";
            });

            document.getElementById("logout-cancel")?.addEventListener("click", function () {
                logoutModal.style.display = "none";
            });

            document.addEventListener("keydown", function (event) {
                if (event.key !== "Escape") return;

                document.querySelectorAll(".modal-overlay.show").forEach(modal => {
                    modal.classList.remove("show");
                    modal.style.display = "none";
                });

                if (logoutModal) logoutModal.style.display = "none";
            });

            document.addEventListener("click", function (event) {
                if (event.target.classList.contains("modal-overlay")) {
                    event.target.classList.remove("show");
                    event.target.style.display = "none";
                }
            });
        });
    </script>
</body>
</html>
