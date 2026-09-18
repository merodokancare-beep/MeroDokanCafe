import os
import sys
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn

def set_cell_background(cell, fill_hex):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex}"/>')
    tcPr.append(shd)

def set_cell_margins(cell, top=120, bottom=120, left=150, right=150):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = parse_xml(f'<w:tcMar {nsdecls("w")}><w:top w:w="{top}" w:type="dxa"/><w:bottom w:w="{bottom}" w:type="dxa"/><w:left w:w="{left}" w:type="dxa"/><w:right w:w="{right}" w:type="dxa"/></w:tcMar>')
    tcPr.append(tcMar)

def set_table_borders(table, color="D0D7DE", sz="4", val="single"):
    tblPr = table._tbl.tblPr
    borders = parse_xml(
        f'<w:tblBorders {nsdecls("w")}>'
        f'<w:top w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>'
        f'<w:left w:val="none"/>'
        f'<w:bottom w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>'
        f'<w:right w:val="none"/>'
        f'<w:insideH w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>'
        f'<w:insideV w:val="none"/>'
        f'</w:tblBorders>'
    )
    tblPr.append(borders)

def create_quotation_document(output_path):
    doc = Document()
    
    # Page setup - 0.75 in margins
    for section in doc.sections:
        section.top_margin = Inches(0.75)
        section.bottom_margin = Inches(0.75)
        section.left_margin = Inches(0.8)
        section.right_margin = Inches(0.8)
        section.page_width = Inches(8.5)
        section.page_height = Inches(11.0)

    # Styling colors
    C_PRIMARY = RGBColor(15, 32, 67)       # Deep Navy
    C_SECONDARY = RGBColor(230, 81, 0)     # Vibrant Cafe Amber/Orange
    C_DARK = RGBColor(33, 37, 41)          # Slate Dark
    C_MUTED = RGBColor(108, 117, 125)      # Muted Gray
    C_WHITE = RGBColor(255, 255, 255)
    HEX_HEADER_BG = "0F2043"
    HEX_ROW_ALT = "F8F9FA"
    HEX_ACCENT_LIGHT = "FFF3E0"

    # Base Normal Style
    style_normal = doc.styles['Normal']
    style_normal.font.name = 'Segoe UI'
    style_normal.font.size = Pt(10)
    style_normal.font.color.rgb = C_DARK

    # --- COVER / HEADER BANNER ---
    header_table = doc.add_table(rows=1, cols=2)
    header_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    header_table.autofit = False
    
    for row in header_table.rows:
        row.cells[0].width = Inches(4.5)
        row.cells[1].width = Inches(2.4)
        set_cell_margins(row.cells[0], top=100, bottom=100, left=100, right=100)
        set_cell_margins(row.cells[1], top=100, bottom=100, left=100, right=100)
    
    # Left: Company / Project Title
    p_title = header_table.rows[0].cells[0].paragraphs[0]
    p_title.paragraph_format.space_after = Pt(2)
    r_main_title = p_title.add_run("MeroDokan Cafe")
    r_main_title.font.name = 'Segoe UI'
    r_main_title.font.size = Pt(22)
    r_main_title.font.bold = True
    r_main_title.font.color.rgb = C_PRIMARY

    p_sub = header_table.rows[0].cells[0].add_paragraph()
    p_sub.paragraph_format.space_after = Pt(0)
    r_sub = p_sub.add_run("Restaurant POS & Kitchen Inventory ERP System")
    r_sub.font.name = 'Segoe UI'
    r_sub.font.size = Pt(11)
    r_sub.font.bold = True
    r_sub.font.color.rgb = C_SECONDARY

    # Right: Document Meta
    p_meta = header_table.rows[0].cells[1].paragraphs[0]
    p_meta.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p_meta.paragraph_format.space_after = Pt(2)
    r_doc_type = p_meta.add_run("PROJECT QUOTATION\n")
    r_doc_type.font.size = Pt(12)
    r_doc_type.font.bold = True
    r_doc_type.font.color.rgb = C_PRIMARY

    r_meta_body = p_meta.add_run(
        "Ref No: QTN-MDK-2026-081\n"
        "Date: September 16, 2026\n"
        "Validity: 30 Days\n"
        "Status: Proposal for Work Order"
    )
    r_meta_body.font.size = Pt(8.5)
    r_meta_body.font.color.rgb = C_MUTED

    # Divider line
    p_div = doc.add_paragraph()
    p_div.paragraph_format.space_before = Pt(8)
    p_div.paragraph_format.space_after = Pt(12)
    r_line = p_div.add_run("―" * 65)
    r_line.font.color.rgb = RGBColor(220, 224, 230)

    # --- CLIENT & VENDOR INFO TABLE ---
    info_table = doc.add_table(rows=1, cols=2)
    info_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    info_table.autofit = False
    info_table.rows[0].cells[0].width = Inches(3.45)
    info_table.rows[0].cells[1].width = Inches(3.45)
    set_cell_background(info_table.rows[0].cells[0], "F4F6F9")
    set_cell_background(info_table.rows[0].cells[1], "F4F6F9")
    set_cell_margins(info_table.rows[0].cells[0], top=140, bottom=140, left=160, right=160)
    set_cell_margins(info_table.rows[0].cells[1], top=140, bottom=140, left=160, right=160)

    # Prepared For
    c0 = info_table.rows[0].cells[0].paragraphs[0]
    c0.paragraph_format.space_after = Pt(3)
    r_c0_h = c0.add_run("PREPARED FOR (CLIENT):\n")
    r_c0_h.font.bold = True
    r_c0_h.font.size = Pt(9.5)
    r_c0_h.font.color.rgb = C_PRIMARY
    r_c0_b = c0.add_run(
        "Client Name / Enterprise: [Client Business Name]\n"
        "Attention: The Management / Owner\n"
        "Location: [City / Address]\n"
        "Contact: [Phone / Email]\n"
        "Project: Cafe POS, Table Management & Inventory ERP"
    )
    r_c0_b.font.size = Pt(8.5)

    # Prepared By
    c1 = info_table.rows[0].cells[1].paragraphs[0]
    c1.paragraph_format.space_after = Pt(3)
    r_c1_h = c1.add_run("PREPARED BY (DEVELOPER / VENDOR):\n")
    r_c1_h.font.bold = True
    r_c1_h.font.size = Pt(9.5)
    r_c1_h.font.color.rgb = C_PRIMARY
    r_c1_b = c1.add_run(
        "Solution Provider: MeroDokan Technologies / Softwares\n"
        "Lead Solutions Architect: Technical Development Team\n"
        "Platform: Desktop & On-Premise Cloud Hybrid ERP\n"
        "Email / Web: support@merodokan.com | +977-98XXXXXXXX\n"
        "Delivery Mode: Complete Turnkey Implementation"
    )
    r_c1_b.font.size = Pt(8.5)

    def add_section_header(title, subtitle=None):
        p = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(14)
        p.paragraph_format.space_after = Pt(3)
        p.paragraph_format.keep_with_next = True
        r = p.add_run(title)
        r.font.name = 'Segoe UI'
        r.font.size = Pt(13)
        r.font.bold = True
        r.font.color.rgb = C_PRIMARY

        if subtitle:
            p_sub = doc.add_paragraph()
            p_sub.paragraph_format.space_before = Pt(0)
            p_sub.paragraph_format.space_after = Pt(6)
            p_sub.paragraph_format.keep_with_next = True
            r_s = p_sub.add_run(subtitle)
            r_s.font.size = Pt(9)
            r_s.font.italic = True
            r_s.font.color.rgb = C_MUTED

    # --- 1. EXECUTIVE SUMMARY ---
    add_section_header("1. Executive Summary & Project Overview", "Purpose-built automation for modern high-volume cafes, bakeries & restaurants")
    p_exec = doc.add_paragraph()
    p_exec.paragraph_format.space_after = Pt(6)
    p_exec.add_run(
        "MeroDokan Cafe is an enterprise-grade, ultra-responsive Restaurant Point of Sale (POS) and "
        "Kitchen Inventory ERP system engineered specifically for high-speed counter service, fine dine-in operations, "
        "and real-time raw material cost control. Designed with an Offline-First Architecture, the system ensures zero operational "
        "downtime during internet or network outages, with instant thermal receipt generation, multi-station KOT routing, "
        "and complete daily raw ingredient audit reconciliation."
    )

    # Key Highlights bullets
    highlights = [
        ("⚡ High-Speed Touch POS & Billing:", " Rapid table billing, split payments, multi-tender settlement (Cash, Fonepay/QR, Cards, Credit/Khata), discount rules, and customized thermal receipts (80mm / 58mm)."),
        ("🪑 Interactive Visual Floor & Table Management:", " Real-time visual table statuses (Available, Occupied, Billed, Reserved) across multiple sections (Dine-in, Terrace, AC Hall, Bar, Garden) with quick table transfers and merge orders."),
        ("👨‍🍳 Kitchen Order Tickets (KOT) & Section Routing:", " Instant automated KOT dispatching to designated preparation stations (Kitchen, Barista Counter, Bakery) with live preparation timer tracking."),
        ("📦 Cafe Raw Material Stock & Daily Usage Hub:", " End-to-end recipe/ingredient tracking, inward delivery GRN recording, daily kitchen consumption logging, and automated evening kitchen stock return with strict limit verification."),
        ("📊 Day-Close Cash Reconciliation & Business Intelligence:", " Automated cashier shift handovers, tender breakdown audits, gross/net revenue analysis, peak-hour heatmaps, and GST tax ledger generation.")
    ]
    for title, desc in highlights:
        p_b = doc.add_paragraph(style='List Bullet')
        p_b.paragraph_format.space_after = Pt(3)
        r_bt = p_b.add_run(title)
        r_bt.font.bold = True
        r_bt.font.color.rgb = C_DARK
        r_bd = p_b.add_run(desc)
        r_bd.font.color.rgb = C_DARK

    # --- 2. DETAILED MODULE BREAKDOWN ---
    add_section_header("2. Comprehensive Scope of Work & Functional Modules", "Itemized functional deliverables included under this turnkey implementation")

    modules = [
        ("Module 1: Interactive Floor Layout & Table Management", [
            "Visual real-time floor plan with dynamic color codes (Green = Available, Red = Occupied, Amber = Bill Printed, Blue = Reserved).",
            "Multi-section floor organization (Main Hall, First Floor, Rooftop, Garden, Barista Counter, VIP Lounge).",
            "One-click table order initiation, running KOT display, guest count tracking, and active waiter assignment.",
            "Seamless table shifting, bill splitting across patrons, and table merging capabilities."
        ]),
        ("Module 2: High-Speed POS Billing & Multi-Tender Payment Engine", [
            "Touchscreen and keyboard-optimized order punching with instant category filtering and quick search.",
            "Special item modifiers, cooking instructions (e.g., 'Extra Spicy', 'Less Sugar', 'Oat Milk'), and add-on pricing.",
            "Multi-tender payment handling: Cash, QR Scan (Fonepay / eSewa / Khalti), Credit/Debit Cards, Customer Ledger Khata, and Complimentary orders.",
            "Customizable invoice formats with cafe branding, tax breakdown (VAT / Pan / Service Charge), customer info, and custom footer greetings."
        ]),
        ("Module 3: Kitchen Display System (KDS) & Multi-Station KOT Routing", [
            "Automated KOT dispatch directly to thermal kitchen printers and live Kitchen Display monitors.",
            "Intelligent routing by preparation department (Hot Kitchen, Cold Kitchen, Barista/Bar, Bakery/Dessert).",
            "Running order status tracking (Pending → Cooking → Ready to Serve → Delivered) to minimize guest wait times.",
            "Item cancellation and order alteration logs with mandatory reason audit."
        ]),
        ("Module 4: Cafe Raw Material Inventory & Daily Usage Hub", [
            "Raw Material Stock Register: Real-time unit stock balances, low-stock threshold alerts, purchase rate tracking, and asset value.",
            "Stock Inward Delivery (+ IN): Purchase GRN logging with Supplier database, invoice numbers, batch tracking, and transport costs.",
            "Daily Kitchen Usage Log (- OUT): Record raw ingredients issued to kitchen/barista with department allocation.",
            "🌙 Close Kitchen / Return Stock (+ IN): Evening stock return with live table of active issued quantities, 1-click selection, and strict limit validation preventing return of unissued quantities.",
            "Movement Audit Ledger: Comprehensive historical ledger with multi-parameter filtering and 1-click CSV/Excel export."
        ]),
        ("Module 5: Day Close Settlement & Cash Drawer Audit", [
            "Shift-wise cash register opening, cash-in/cash-out tracking, and end-of-day Z-Report closing settlement.",
            "Automated tender reconciliation comparing system billing totals with actual physical cash counted (Over/Short detection).",
            "Manager shift sign-off and tamper-evident daily financial lock."
        ]),
        ("Module 6: Business Intelligence, Analytics & GST Reports", [
            "Real-time Executive Dashboard: Gross Sales, Net Revenue, Tax Collected, Discount Expense, and Average Ticket Size.",
            "Menu Engineering: Best-selling items, high-margin contributors, least-moving items, and peak service hour analysis.",
            "Raw Material Consumption Report: Reconciled Issued vs. Returned vs. Net Consumed quantities and net ingredient costs.",
            "Tax & Audit Reports: VAT / Pan sales registers, cancellation logs, bill reprint audit logs, and discounted order records."
        ]),
        ("Module 7: Master Data Management & Security Administration", [
            "Complete Menu Master: Unlimited categories, menu items, variants (Regular, Large), add-on groups, and active pricing.",
            "Role-Based Access Control (RBAC): Distinct permissions for Administrator, Cashier, Captain/Waiter, Chef, and Storekeeper.",
            "Offline-first SQL Database with automated 1-click local & cloud backup utility to prevent any data loss."
        ])
    ]

    for mod_title, mod_items in modules:
        p_m = doc.add_paragraph()
        p_m.paragraph_format.space_before = Pt(8)
        p_m.paragraph_format.space_after = Pt(2)
        p_m.paragraph_format.keep_with_next = True
        r_mt = p_m.add_run(f"■ {mod_title}")
        r_mt.font.bold = True
        r_mt.font.size = Pt(10.5)
        r_mt.font.color.rgb = C_PRIMARY

        for item in mod_items:
            p_mi = doc.add_paragraph(style='List Bullet')
            p_mi.paragraph_format.space_after = Pt(2)
            r_mii = p_mi.add_run(item)
            r_mii.font.size = Pt(9.5)

    # --- 3. HARDWARE COMPATIBILITY & ARCHITECTURE ---
    add_section_header("3. Architecture & Hardware Compatibility", "Built for high reliability, zero lag, and flexible hardware deployment")
    
    arch_table = doc.add_table(rows=5, cols=2)
    arch_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(arch_table)
    arch_table.autofit = False
    for r in arch_table.rows:
        r.cells[0].width = Inches(2.2)
        r.cells[1].width = Inches(4.7)
        set_cell_margins(r.cells[0], top=80, bottom=80, left=100, right=100)
        set_cell_margins(r.cells[1], top=80, bottom=80, left=100, right=100)

    arch_data = [
        ("Deployment Model", "Offline-First On-Premise System (Zero downtime during ISP/Internet outages)"),
        ("Supported Operating Systems", "Windows 10 / Windows 11 / Windows Server (Touchscreen & Desktop compatible)"),
        ("POS Receipt Printers", "Standard 80mm & 58mm Thermal Printers (USB, Ethernet/LAN, Wi-Fi, Bluetooth)"),
        ("Kitchen / Bar Printers", "Network LAN / Wi-Fi Thermal KOT Printers and Kitchen Display Monitors (KDS)"),
        ("Peripherals Supported", "Standard Cash Drawers (RJ11), Barcode/QR Scanners, Customer Facing Displays")
    ]
    for i, (k, v) in enumerate(arch_data):
        row = arch_table.rows[i]
        if i % 2 == 0:
            set_cell_background(row.cells[0], HEX_ROW_ALT)
            set_cell_background(row.cells[1], HEX_ROW_ALT)
        p0 = row.cells[0].paragraphs[0]
        r0 = p0.add_run(k)
        r0.font.bold = True
        r0.font.size = Pt(9)
        p1 = row.cells[1].paragraphs[0]
        r1 = p1.add_run(v)
        r1.font.size = Pt(9)

    # --- 4. COMMERCIAL QUOTATION / PRICING BREAKDOWN ---
    add_section_header("4. Commercial Quotation & Investment Schedule", "Transparent itemized investment for software development, setup & licensing")

    quote_table = doc.add_table(rows=8, cols=4)
    quote_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(quote_table)
    quote_table.autofit = False
    
    col_widths = [Inches(0.6), Inches(3.6), Inches(1.3), Inches(1.4)]
    for row in quote_table.rows:
        for idx, w in enumerate(col_widths):
            row.cells[idx].width = w
            set_cell_margins(row.cells[idx], top=100, bottom=100, left=100, right=100)

    # Header Row
    headers = ["S.N.", "Description of Deliverable & Service Scope", "Quantity / Unit", "Amount (NPR / INR)"]
    for idx, text in enumerate(headers):
        cell = quote_table.rows[0].cells[idx]
        set_cell_background(cell, HEX_HEADER_BG)
        p = cell.paragraphs[0]
        if idx in [0, 2]: p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        elif idx == 3: p.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        r = p.add_run(text)
        r.font.bold = True
        r.font.size = Pt(9)
        r.font.color.rgb = C_WHITE

    items = [
        ("1", "MeroDokan Cafe Core Software License\n• Complete POS Billing Engine & Floor/Table Visual System\n• Kitchen Order Ticket (KOT) & Kitchen Display routing\n• Day Close Settlement & Z-Report Reconciliations", "1 Full Setup\n(Lifetime / Term)", "45,000.00"),
        ("2", "Raw Material & Kitchen Usage ERP Hub\n• Ingredient Stock Register & Low Stock Alerts\n• Inward GRN Delivery & Supplier Ledger\n• Evening Kitchen Close & Return Stock Validation Module", "1 Module\n(Integrated)", "20,000.00"),
        ("3", "Installation, On-Site Deployment & Hardware Integration\n• Database configuration & thermal printer setup (Counter + KOT)\n• Network sharing & multi-terminal connectivity\n• Cash drawer & barcode scanner integration", "On-Site\nDeployment", "10,000.00"),
        ("4", "Menu Master Digitization & Master Data Configuration\n• Entry of all food categories, menu items, recipes & add-ons\n• Table floor layout design and department tagging", "Full Menu\nDigitization", "5,000.00"),
        ("5", "Staff Onboarding & Operational Training\n• Hands-on training for Cashiers, Captains, Waiters & Kitchen Staff\n• Admin & Storekeeper management training", "2 Comprehensive\nSessions", "5,000.00"),
        ("6", "1-Year Premium Warranty & Technical Support\n• Free software updates, bug fixes, database health checkups\n• Remote emergency priority support & automated backup setup", "12 Months\nIncluded", "Included / Free\n(1st Year)")
    ]

    for idx, (sn, desc, qty, amt) in enumerate(items, start=1):
        row = quote_table.rows[idx]
        if idx % 2 == 1:
            for cell in row.cells: set_cell_background(cell, HEX_ROW_ALT)
        
        p0 = row.cells[0].paragraphs[0]
        p0.alignment = WD_ALIGN_PARAGRAPH.CENTER
        r0 = p0.add_run(sn)
        r0.font.size = Pt(8.5)

        p1 = row.cells[1].paragraphs[0]
        r1 = p1.add_run(desc)
        r1.font.size = Pt(8.5)

        p2 = row.cells[2].paragraphs[0]
        p2.alignment = WD_ALIGN_PARAGRAPH.CENTER
        r2 = p2.add_run(qty)
        r2.font.size = Pt(8.5)

        p3 = row.cells[3].paragraphs[0]
        p3.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        r3 = p3.add_run(amt)
        r3.font.bold = True
        r3.font.size = Pt(8.5)

    # Total Summary Row
    total_row = quote_table.rows[7]
    set_cell_background(total_row.cells[0], HEX_ACCENT_LIGHT)
    set_cell_background(total_row.cells[1], HEX_ACCENT_LIGHT)
    set_cell_background(total_row.cells[2], HEX_ACCENT_LIGHT)
    set_cell_background(total_row.cells[3], HEX_ACCENT_LIGHT)

    p_tot_lbl = total_row.cells[1].paragraphs[0]
    r_tot_lbl = p_tot_lbl.add_run("TOTAL PROJECT INVESTMENT (Turnkey Implementation)")
    r_tot_lbl.font.bold = True
    r_tot_lbl.font.size = Pt(9.5)
    r_tot_lbl.font.color.rgb = C_PRIMARY

    p_tot_val = total_row.cells[3].paragraphs[0]
    p_tot_val.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    r_tot_val = p_tot_val.add_run("Rs. 85,000.00")
    r_tot_val.font.bold = True
    r_tot_val.font.size = Pt(11)
    r_tot_val.font.color.rgb = C_SECONDARY

    p_note = doc.add_paragraph()
    p_note.paragraph_format.space_before = Pt(4)
    p_note.paragraph_format.space_after = Pt(6)
    r_n = p_note.add_run("*(Note: Prices can be adjusted based on the client's custom currency, multi-branch licensing, or specific hardware bundle requirements).*")
    r_n.font.size = Pt(8)
    r_n.font.italic = True
    r_n.font.color.rgb = C_MUTED

    # --- 5. IMPLEMENTATION TIMELINE & MILESTONES ---
    add_section_header("5. Project Implementation Timeline & Milestone Schedule", "Target turnaround time: 7 to 14 working days from Work Order issuance")

    time_table = doc.add_table(rows=5, cols=3)
    time_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(time_table)
    time_table.autofit = False
    
    time_widths = [Inches(1.5), Inches(3.8), Inches(1.6)]
    for row in time_table.rows:
        for idx, w in enumerate(time_widths):
            row.cells[idx].width = w
            set_cell_margins(row.cells[idx], top=80, bottom=80, left=100, right=100)

    t_headers = ["Phase / Timeline", "Key Implementation Milestones", "Payment Milestone"]
    for idx, text in enumerate(t_headers):
        cell = time_table.rows[0].cells[idx]
        set_cell_background(cell, HEX_HEADER_BG)
        p = cell.paragraphs[0]
        if idx == 0: p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        elif idx == 2: p.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        r = p.add_run(text)
        r.font.bold = True
        r.font.size = Pt(8.5)
        r.font.color.rgb = C_WHITE

    milestones = [
        ("Phase 1: Day 1 - 3", "Work order confirmation, requirement freeze, database provisioning, and menu master structure setup.", "30% Advance\nwith Work Order"),
        ("Phase 2: Day 4 - 8", "System deployment, menu item entry, recipe formulation, KOT printer configuration, and floor layout mapping.", "40% Upon Beta\nDeployment"),
        ("Phase 3: Day 9 - 11", "Hardware integration (thermal printers, cash drawer, network testing), cashier & staff training, mock billing run.", "20% Upon User\nAcceptance (UAT)"),
        ("Phase 4: Day 12 - 14", "Official Go-Live in live operations, handover of admin credentials, backup scheduler activation, warranty commencement.", "10% Final Settlement\n(Go-Live)")
    ]

    for idx, (ph, desc, pay) in enumerate(milestones, start=1):
        row = time_table.rows[idx]
        if idx % 2 == 1:
            for cell in row.cells: set_cell_background(cell, HEX_ROW_ALT)
        
        p0 = row.cells[0].paragraphs[0]
        r0 = p0.add_run(ph)
        r0.font.bold = True
        r0.font.size = Pt(8.5)

        p1 = row.cells[1].paragraphs[0]
        r1 = p1.add_run(desc)
        r1.font.size = Pt(8.5)

        p2 = row.cells[2].paragraphs[0]
        p2.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        r2 = p2.add_run(pay)
        r2.font.bold = True
        r2.font.size = Pt(8.5)

    # --- 6. WARRANTY, SLA & SUPPORT ---
    add_section_header("6. Warranty, Backup Security & Support SLA", "Guaranteed peace of mind with round-the-clock priority assistance")
    
    sla_points = [
        ("🛡️ Comprehensive 12-Month Free Warranty:", " Includes all critical software patches, bug resolution, performance fine-tuning, and logic adjustments without additional fees."),
        ("⚡ High-Priority Support Response:", " Critical POS/Billing downtime tickets responded within 30 minutes. General queries resolved within 4 working hours via AnyDesk/TeamViewer or on-site visit."),
        ("🔒 Automated Database Backup System:", " Automated daily backup scheduler configured on the main terminal with secondary drive replication to ensure 100% data preservation against hardware failures."),
        ("🔄 Free Minor Version Upgrades:", " All standard UI and feature enhancements rolled out within the warranty period are provided free of cost.")
    ]
    for title, desc in sla_points:
        p_sla = doc.add_paragraph(style='List Bullet')
        p_sla.paragraph_format.space_after = Pt(3)
        r_st = p_sla.add_run(title)
        r_st.font.bold = True
        r_st.font.color.rgb = C_DARK
        r_sd = p_sla.add_run(desc)
        r_sd.font.color.rgb = C_DARK

    # --- 7. TERMS & CONDITIONS ---
    add_section_header("7. Standard Terms & Conditions", "Governing provisions for project execution and work order issuance")
    
    terms = [
        "Hardware procurement (Printers, Terminals, LAN Cables, Cash Drawers) shall be provided by the client, or can be supplied by the vendor as a separate hardware quotation.",
        "Any major change requests outside the documented functional scope will be mutually evaluated with a separate change-request timeline & quote.",
        "The software license is granted for use at the designated cafe premises with unlimited lifetime usage under on-premise local setup.",
        "This quotation is strictly valid for 30 calendar days from the date of issuance."
    ]
    for idx, t in enumerate(terms, start=1):
        p_t = doc.add_paragraph()
        p_t.paragraph_format.space_after = Pt(2)
        r_ti = p_t.add_run(f"7.{idx}  ")
        r_ti.font.bold = True
        r_ti.font.size = Pt(8.5)
        r_tb = p_t.add_run(t)
        r_tb.font.size = Pt(8.5)

    # --- 8. WORK ORDER ACCEPTANCE / SIGNATURE BLOCK ---
    add_section_header("8. Work Order Acceptance & Authorization", "Signing below converts this quotation into a legally binding Work Order")

    sign_table = doc.add_table(rows=1, cols=2)
    sign_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    sign_table.autofit = False
    sign_table.rows[0].cells[0].width = Inches(3.45)
    sign_table.rows[0].cells[1].width = Inches(3.45)
    set_cell_background(sign_table.rows[0].cells[0], "FAFAFA")
    set_cell_background(sign_table.rows[0].cells[1], "FAFAFA")
    set_cell_margins(sign_table.rows[0].cells[0], top=120, bottom=120, left=140, right=140)
    set_cell_margins(sign_table.rows[0].cells[1], top=120, bottom=120, left=140, right=140)

    # Client Signature Box
    p_sc = sign_table.rows[0].cells[0].paragraphs[0]
    p_sc.paragraph_format.space_after = Pt(2)
    r_sc_h = p_sc.add_run("ACCEPTED & CONFIRMED BY (CLIENT):\n\n")
    r_sc_h.font.bold = True
    r_sc_h.font.size = Pt(9)
    r_sc_h.font.color.rgb = C_PRIMARY
    r_sc_b = p_sc.add_run(
        "Authorized Signature: _______________________\n\n"
        "Full Name: ________________________________\n"
        "Designation: ______________________________\n"
        "Enterprise / Cafe Seal: ____________________\n"
        "Date of Work Order: ________________________"
    )
    r_sc_b.font.size = Pt(8.5)

    # Vendor Signature Box
    p_sv = sign_table.rows[0].cells[1].paragraphs[0]
    p_sv.paragraph_format.space_after = Pt(2)
    r_sv_h = p_sv.add_run("AUTHORIZED SIGNATORY (VENDOR):\n\n")
    r_sv_h.font.bold = True
    r_sv_h.font.size = Pt(9)
    r_sv_h.font.color.rgb = C_PRIMARY
    r_sv_b = p_sv.add_run(
        "Authorized Signature: _______________________\n\n"
        "Name: Solutions Project Lead\n"
        "Designation: Principal Technical Consultant\n"
        "Company Seal: MeroDokan Technologies\n"
        "Date of Issuance: September 16, 2026"
    )
    r_sv_b.font.size = Pt(8.5)

    doc.save(output_path)
    print(f"Quotation successfully generated at: {output_path}")

if __name__ == "__main__":
    out_file = r"d:\Bhawani Works\Project All\MeroDokanCafe\MeroDokan_Cafe_Project_Quotation.docx"
    create_quotation_document(out_file)
