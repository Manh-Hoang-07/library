using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Library
{
    public class BooksPanel : Panel
    {
        TextBox txtName, txtAuthor, txtCategory, txtQuantity;
        DataGridView dgv;
        int selectedId = -1;

        public BooksPanel()
        {
            BackColor = Color.FromArgb(242, 246, 251);
            Padding   = new Padding(24, 20, 24, 16);
            Build();
            LoadData();
        }

        // ── Build UI ─────────────────────────────────────────────────────────
        void Build()
        {
            // Title
            var title = new Label
            {
                Text      = "Book Management",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 44, 80),
                Dock      = DockStyle.Top,
                Height    = 52,
                Padding   = new Padding(2, 8, 0, 0)
            };

            // ---- form card ----
            var card = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 120,
                BackColor = Color.White,
                Padding   = new Padding(16, 10, 16, 10)
            };
            card.Paint += CardBorder;

            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 2
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            txtName     = MakeTxt();
            txtAuthor   = MakeTxt();
            txtCategory = MakeTxt();
            txtQuantity = MakeTxt("1");

            tbl.Controls.Add(Lbl("Name:"),     0, 0); tbl.Controls.Add(txtName,     1, 0);
            tbl.Controls.Add(Lbl("Author:"),   2, 0); tbl.Controls.Add(txtAuthor,   3, 0);
            tbl.Controls.Add(Lbl("Category:"), 0, 1); tbl.Controls.Add(txtCategory, 1, 1);
            tbl.Controls.Add(Lbl("In Stock:"), 2, 1); tbl.Controls.Add(txtQuantity, 3, 1);
            card.Controls.Add(tbl);

            // ---- button bar ----
            var btnBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.White,
                Padding   = new Padding(16, 7, 0, 7)
            };
            btnBar.Paint += CardBorder;

            var flow = new FlowLayoutPanel
            {
                Dock         = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor    = Color.Transparent,
                WrapContents = false
            };

            var btnAdd    = Btn("＋ Add",    Color.FromArgb(39, 174, 96));
            var btnUpdate = Btn("✎ Update", Color.FromArgb(52, 152, 219));
            var btnDelete = Btn("✕ Delete", Color.FromArgb(231, 76, 60));
            var btnClear  = Btn("↺ Clear",  Color.FromArgb(149, 165, 166));

            btnAdd.Click    += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnClear.Click  += (s, e) => ClearForm();

            flow.Controls.Add(btnAdd);
            flow.Controls.Add(btnUpdate);
            flow.Controls.Add(btnDelete);
            flow.Controls.Add(btnClear);
            btnBar.Controls.Add(flow);

            // ---- grid ----
            dgv = new DataGridView { Dock = DockStyle.Fill };
            StyleGrid(dgv);
            dgv.SelectionChanged += Dgv_SelectionChanged;

            // gap between button bar and grid
            var gap = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };

            // WinForms processes Controls in REVERSE index order for docking.
            // Add Fill first (lowest index = processed last = fills remaining space).
            // Add Top controls in reverse visual order (last added = topmost on screen).
            Controls.Add(dgv);     // Fill  → index 0 → processed last → fills bottom area
            Controls.Add(gap);     // Top   → index 1
            Controls.Add(btnBar);  // Top   → index 2
            Controls.Add(card);    // Top   → index 3
            Controls.Add(title);   // Top   → index 4 → processed first → appears at TOP
        }

        // ── Data ─────────────────────────────────────────────────────────────
        public void LoadData()
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    var dt = new DataTable();
                    new MySqlDataAdapter(
                        "SELECT Id, Name, Author, Category, Quantity FROM Books ORDER BY Name",
                        conn).Fill(dt);
                    dgv.DataSource = dt;
                }
                ApplyGridHeaders();
            }
            catch (Exception ex)
            {
                Err("Error loading books", ex);
            }
        }

        void ApplyGridHeaders()
        {
            if (dgv.Columns.Count == 0) return;
            Set("Id",       "ID",        30);
            Set("Name",     "Book Name", 120);
            Set("Author",   "Author",    100);
            Set("Category", "Category",  80);
            Set("Quantity", "In Stock",  40);

            void Set(string col, string hdr, int weight)
            {
                if (dgv.Columns.Contains(col))
                {
                    dgv.Columns[col].HeaderText = hdr;
                    dgv.Columns[col].FillWeight = weight;
                }
            }
        }

        void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            var row = dgv.CurrentRow;
            if (row == null) return;
            try
            {
                selectedId          = Convert.ToInt32(row.Cells["Id"].Value);
                txtName.Text        = row.Cells["Name"].Value?.ToString()     ?? "";
                txtAuthor.Text      = row.Cells["Author"].Value?.ToString()   ?? "";
                txtCategory.Text    = row.Cells["Category"].Value?.ToString() ?? "";
                txtQuantity.Text    = row.Cells["Quantity"].Value?.ToString() ?? "0";
            }
            catch { /* ignore during rebind */ }
        }

        // ── CRUD ─────────────────────────────────────────────────────────────
        void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!Validate(out int qty)) return;

            if (IsDuplicateBook(txtName.Text.Trim(), txtAuthor.Text.Trim()))
            {
                Info("Sách này (cùng tên + tác giả) đã tồn tại trong hệ thống."); return;
            }
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO Books (Name,Author,Category,Quantity) VALUES (@n,@a,@c,@q)",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("@n", txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@a", txtAuthor.Text.Trim());
                        cmd.Parameters.AddWithValue("@c", txtCategory.Text.Trim());
                        cmd.Parameters.AddWithValue("@q", qty);
                        cmd.ExecuteNonQuery();
                    }
                }
                ClearForm(); LoadData();
            }
            catch (Exception ex) { Err("Lỗi thêm sách", ex); }
        }

        void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedId < 0) { Info("Chọn một sách trước."); return; }
            if (!Validate(out int qty)) return;

            if (IsDuplicateBook(txtName.Text.Trim(), txtAuthor.Text.Trim(), selectedId))
            {
                Info("Sách này (cùng tên + tác giả) đã tồn tại trong hệ thống."); return;
            }

            int borrowed = CurrentlyBorrowed(selectedId);
            if (qty < borrowed)
            {
                Info($"Không thể đặt số lượng = {qty}.\n" +
                     $"Hiện có {borrowed} quyển đang được mượn, số lượng tối thiểu là {borrowed}."); return;
            }
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "UPDATE Books SET Name=@n,Author=@a,Category=@c,Quantity=@q WHERE Id=@id",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("@n",  txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@a",  txtAuthor.Text.Trim());
                        cmd.Parameters.AddWithValue("@c",  txtCategory.Text.Trim());
                        cmd.Parameters.AddWithValue("@q",  qty);
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                ClearForm(); LoadData();
            }
            catch (Exception ex) { Err("Lỗi cập nhật sách", ex); }
        }

        void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedId < 0) { Info("Chọn một sách trước."); return; }

            int borrowed = CurrentlyBorrowed(selectedId);
            if (borrowed > 0)
            {
                Info($"Không thể xóa: sách này hiện có {borrowed} quyển đang được mượn.\n" +
                     "Vui lòng chờ trả đủ rồi mới xóa."); return;
            }
            if (MessageBox.Show("Xóa sách này?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("DELETE FROM Books WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                ClearForm(); LoadData();
            }
            catch (Exception ex) { Err("Lỗi xóa sách", ex); }
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        bool Validate(out int qty)
        {
            qty = 0;
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                Info("Tên sách không được để trống."); txtName.Focus(); return false;
            }
            if (!int.TryParse(txtQuantity.Text, out qty) || qty < 0)
            {
                Info("Số lượng phải là số nguyên >= 0."); txtQuantity.Focus(); return false;
            }
            return true;
        }

        // Returns true if another book with same Name+Author exists (excludes self on update)
        bool IsDuplicateBook(string name, string author, int excludeId = -1)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM Books " +
                    "WHERE LOWER(TRIM(Name))=LOWER(TRIM(@n)) " +
                    "  AND LOWER(TRIM(Author))=LOWER(TRIM(@a)) " +
                    "  AND Id <> @id", conn))
                {
                    cmd.Parameters.AddWithValue("@n",  name);
                    cmd.Parameters.AddWithValue("@a",  author);
                    cmd.Parameters.AddWithValue("@id", excludeId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        // Returns total copies currently out on active borrows
        int CurrentlyBorrowed(int bookId)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "SELECT COALESCE(SUM(bd.Quantity),0) " +
                    "FROM BorrowDetails bd " +
                    "JOIN Borrows b ON b.Id = bd.BorrowId " +
                    "WHERE bd.BookId = @id AND b.Status = 'borrowing'", conn))
                {
                    cmd.Parameters.AddWithValue("@id", bookId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        void ClearForm()
        {
            selectedId       = -1;
            txtName.Text     = "";
            txtAuthor.Text   = "";
            txtCategory.Text = "";
            txtQuantity.Text = "1";
            dgv.ClearSelection();
        }

        internal static void CardBorder(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var p = (Panel)sender;
            using (var pen = new Pen(Color.FromArgb(218, 226, 240)))
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }

        // Alias so UsersPanel / BorrowPanel can reuse without extra class
        internal static void CardBorder2(object sender, System.Windows.Forms.PaintEventArgs e)
            => CardBorder(sender, e);

        static TextBox MakeTxt(string val = "")
            => new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10f), Text = val };

        static Label Lbl(string text) => new Label
        {
            Text      = text,
            TextAlign = ContentAlignment.MiddleRight,
            Dock      = DockStyle.Fill,
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(80, 100, 130),
            Padding   = new Padding(0, 0, 8, 0)
        };

        static Button Btn(string text, Color bg)
        {
            var b = new Button
            {
                Text      = text,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f),
                Size      = new Size(98, 34),
                Margin    = new Padding(0, 0, 8, 0),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        internal static void StyleGrid(DataGridView g)
        {
            g.Font                       = new Font("Segoe UI", 9.5f);
            g.RowTemplate.Height         = 34;
            g.AutoSizeColumnsMode        = DataGridViewAutoSizeColumnsMode.Fill;
            g.AllowUserToAddRows         = false;
            g.AllowUserToDeleteRows      = false;
            g.MultiSelect                = false;
            g.ReadOnly                   = true;
            g.SelectionMode              = DataGridViewSelectionMode.FullRowSelect;
            g.RowHeadersVisible          = false;
            g.EnableHeadersVisualStyles  = false;
            g.BorderStyle                = BorderStyle.None;
            g.GridColor                  = Color.FromArgb(220, 228, 240);
            g.BackgroundColor            = Color.White;

            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(26, 39, 68);
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            g.ColumnHeadersDefaultCellStyle.Padding   = new Padding(6, 0, 0, 0);
            g.ColumnHeadersHeight                     = 40;
            g.ColumnHeadersBorderStyle                = DataGridViewHeaderBorderStyle.None;

            g.DefaultCellStyle.BackColor          = Color.White;
            g.DefaultCellStyle.ForeColor          = Color.FromArgb(50, 60, 80);
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            g.DefaultCellStyle.SelectionForeColor = Color.White;
            g.DefaultCellStyle.Padding            = new Padding(6, 0, 0, 0);
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 246, 255);
        }

        static void Info(string msg)
            => MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

        static void Err(string ctx, Exception ex)
            => MessageBox.Show(ctx + ":\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
