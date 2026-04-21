using System;
using System.Drawing;
using System.Windows.Forms;

namespace Library
{
    public class MainForm : Form
    {
        // ── Palette ──────────────────────────────────────────────────────────
        static readonly Color NavBg      = Color.FromArgb(26, 39, 68);
        static readonly Color NavBgDark  = Color.FromArgb(18, 28, 50);
        static readonly Color NavActive  = Color.FromArgb(52, 152, 219);
        static readonly Color NavHover   = Color.FromArgb(44, 62, 96);
        static readonly Color NavTextDim = Color.FromArgb(150, 180, 220);

        private Button btnNavBooks, btnNavUsers, btnNavBorrow;
        private Button activeBtn;
        private BooksPanel  booksPanel;
        private UsersPanel  usersPanel;
        private BorrowPanel borrowPanel;
        private Panel pnlContent;

        public MainForm()
        {
            Text          = "Library Management System";
            Size          = new Size(1160, 720);
            MinimumSize   = new Size(960, 580);
            StartPosition = FormStartPosition.CenterScreen;
            Font          = new Font("Segoe UI", 9.5f);
            BackColor     = Color.White;

            // IMPORTANT: Fill control must be added FIRST (lower index),
            // then Left/Right/Top controls AFTER (higher index).
            // WinForms layout processes in reverse index order, so higher-index
            // docked controls claim their edge first, Fill takes whatever remains.
            BuildContent(); // pnlContent → index 0 (Fill, processed last)
            BuildSidebar(); // sidebar    → index 1 (Left, processed first → takes left edge)

            NavigateTo(booksPanel, btnNavBooks);
        }

        // ── Sidebar ───────────────────────────────────────────────────────────
        void BuildSidebar()
        {
            var sidebar = new Panel
            {
                Width     = 215,
                Dock      = DockStyle.Left,
                BackColor = NavBg
            };
            Controls.Add(sidebar); // added SECOND → index 1 → processed first → takes left 215px

            // header (only one DockStyle.Top in sidebar → no ordering conflict)
            var header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = NavBgDark };
            var logo   = new Label
            {
                Text      = "📚  Library System",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(55, 80, 120) };
            header.Controls.Add(logo);
            header.Controls.Add(sep);

            // nav buttons in FlowLayoutPanel (unambiguous top-to-bottom order)
            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 14, 0, 0)
            };

            btnNavBooks  = MakeNavBtn("  📚   Books");
            btnNavUsers  = MakeNavBtn("  👤   Users");
            btnNavBorrow = MakeNavBtn("  🔄   Borrow / Return");

            btnNavBooks.Click  += (s, e) => NavigateTo(booksPanel,  btnNavBooks);
            btnNavUsers.Click  += (s, e) => NavigateTo(usersPanel,  btnNavUsers);
            btnNavBorrow.Click += (s, e) => { borrowPanel.Reload(); NavigateTo(borrowPanel, btnNavBorrow); };

            flow.Controls.Add(btnNavBooks);
            flow.Controls.Add(btnNavUsers);
            flow.Controls.Add(btnNavBorrow);

            // version label (only one DockStyle.Bottom → no conflict)
            var ver = new Label
            {
                Text      = "v1.0  ·  Level 1",
                ForeColor = Color.FromArgb(80, 110, 155),
                Font      = new Font("Segoe UI", 8f),
                Dock      = DockStyle.Bottom,
                Height    = 28,
                TextAlign = ContentAlignment.MiddleCenter
            };

            sidebar.Controls.Add(header);
            sidebar.Controls.Add(flow);
            sidebar.Controls.Add(ver);
        }

        Button MakeNavBtn(string text)
        {
            var btn = new Button
            {
                Text      = text,
                FlatStyle = FlatStyle.Flat,
                ForeColor = NavTextDim,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Width     = 215,
                Height    = 52,
                Font      = new Font("Segoe UI", 10.5f),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0)
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = NavHover;
            return btn;
        }

        // ── Content ───────────────────────────────────────────────────────────
        void BuildContent()
        {
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(242, 246, 251) };
            Controls.Add(pnlContent); // added FIRST → index 0 → processed last → fills remaining space

            booksPanel  = new BooksPanel  { Dock = DockStyle.Fill, Visible = false };
            usersPanel  = new UsersPanel  { Dock = DockStyle.Fill, Visible = false };
            borrowPanel = new BorrowPanel { Dock = DockStyle.Fill, Visible = false };

            pnlContent.Controls.Add(booksPanel);
            pnlContent.Controls.Add(usersPanel);
            pnlContent.Controls.Add(borrowPanel);
        }

        // ── Navigation ────────────────────────────────────────────────────────
        void NavigateTo(Panel target, Button navBtn)
        {
            foreach (Control c in pnlContent.Controls) c.Visible = false;
            if (target != null) target.Visible = true;

            if (activeBtn != null)
            {
                activeBtn.BackColor = Color.Transparent;
                activeBtn.ForeColor = NavTextDim;
            }
            if (navBtn != null)
            {
                navBtn.BackColor = NavActive;
                navBtn.ForeColor = Color.White;
                activeBtn = navBtn;
            }
        }
    }
}
