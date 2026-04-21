using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Library
{
    public class BorrowPanel : Panel
    {
        // ── Form fields ───────────────────────────────────────────────────────
        ComboBox cmbUser, cmbBook;
        NumericUpDown nudQty;
        DateTimePicker dtpBorrowDate;
        Label lblAvail, lblMode;
        Button btnSave, btnDelete, btnClear;

        DataGridView dgv;
        int editingBorrowId = -1;   // -1 = create mode, else = edit mode

        public BorrowPanel()
        {
            BackColor = Color.FromArgb(242, 246, 251);
            Padding   = new Padding(24, 20, 24, 16);
            Build();
        }

        // ── Build UI ─────────────────────────────────────────────────────────
        void Build()
        {
            // Page title
            var title = new Label
            {
                Text      = "Borrow Records  —  Quản lý phiếu mượn",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 44, 80),
                Dock      = DockStyle.Top,
                Height    = 50,
                Padding   = new Padding(2, 6, 0, 0)
            };

            // ── Form card ────────────────────────────────────────────────────
            var card = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 150,
                BackColor = Color.White,
                Padding   = new Padding(16, 10, 16, 10)
            };
            card.Paint += BooksPanel.CardBorder2;

            lblMode = new Label
            {
                Text      = "✚  Tạo phiếu mượn mới",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(39, 174, 96),
                Dock      = DockStyle.Top,
                Height    = 22
            };

            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 3
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 34));

            cmbUser = new ComboBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10f), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbBook = new ComboBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10f), DropDownStyle = ComboBoxStyle.DropDownList };
            nudQty  = new NumericUpDown { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10f), Minimum = 1, Maximum = 999, Value = 1 };
            dtpBorrowDate = new DateTimePicker { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10f), Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            lblAvail = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f, FontStyle.Italic), ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleLeft };

            cmbBook.SelectedIndexChanged += (s, e) => UpdateAvail();

            tbl.Controls.Add(Lbl("Người mượn:"), 0, 0); tbl.Controls.Add(cmbUser,       1, 0);
            tbl.Controls.Add(Lbl("Ngày mượn:"),  2, 0); tbl.Controls.Add(dtpBorrowDate, 3, 0);
            tbl.Controls.Add(Lbl("Sách:"),        0, 1); tbl.Controls.Add(cmbBook,       1, 1);
            tbl.Controls.Add(Lbl("Còn lại:"),    2, 1); tbl.Controls.Add(lblAvail,      3, 1);
            tbl.Controls.Add(Lbl("Số lượng:"),   0, 2); tbl.Controls.Add(nudQty,        1, 2);

            card.Controls.Add(tbl);
            card.Controls.Add(lblMode);   // higher index → topmost in card

            // ── Button bar ───────────────────────────────────────────────────
            var btnBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.White,
                Padding   = new Padding(16, 7, 0, 7)
            };
            btnBar.Paint += BooksPanel.CardBorder2;

            var flow = new FlowLayoutPanel
            {
                Dock         = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor    = Color.Transparent,
                WrapContents = false
            };

            btnSave   = Btn("✚  Tạo phiếu",    Color.FromArgb(39, 174, 96));
            btnDelete = Btn("✕  Xóa phiếu",    Color.FromArgb(231, 76, 60));
            var btnReturn  = Btn("📥  Trả sách",   Color.FromArgb(211, 84, 0));
            btnClear  = Btn("↺  Làm mới",      Color.FromArgb(149, 165, 166));
            var btnRefresh = Btn("🔄  Tải lại",    Color.FromArgb(52, 73, 94));

            btnSave.Click    += BtnSave_Click;
            btnDelete.Click  += BtnDelete_Click;
            btnReturn.Click  += BtnReturn_Click;
            btnClear.Click   += (s, e) => SetCreateMode();
            btnRefresh.Click += (s, e) => Reload();

            flow.Controls.Add(btnSave);
            flow.Controls.Add(btnDelete);
            flow.Controls.Add(btnReturn);
            flow.Controls.Add(btnClear);
            flow.Controls.Add(btnRefresh);
            btnBar.Controls.Add(flow);

            // ── History grid ─────────────────────────────────────────────────
            dgv = new DataGridView { Dock = DockStyle.Fill };
            BooksPanel.StyleGrid(dgv);
            dgv.SelectionChanged += Dgv_SelectionChanged;
            dgv.CellFormatting   += Dgv_CellFormatting;

            var gap = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };

            // Reverse-index: Fill first → top controls claim space top-down
            Controls.Add(dgv);
            Controls.Add(gap);
            Controls.Add(btnBar);
            Controls.Add(card);
            Controls.Add(title);
        }

        // ── Data loading ──────────────────────────────────────────────────────
        public void Reload()
        {
            LoadUsers();
            LoadBooks();
            LoadBorrows();
            SetCreateMode();
        }

        void LoadUsers()
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    var dt = new DataTable();
                    new MySqlDataAdapter("SELECT Id, Name FROM Users ORDER BY Name", conn).Fill(dt);
                    cmbUser.DataSource    = dt;
                    cmbUser.DisplayMember = "Name";
                    cmbUser.ValueMember   = "Id";
                }
            }
            catch (Exception ex) { Err("Lỗi tải người dùng", ex); }
        }

        void LoadBooks()
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    var dt = new DataTable();
                    new MySqlDataAdapter("SELECT Id, Name, Quantity FROM Books ORDER BY Name", conn).Fill(dt);

                    dt.Columns.Add("Display", typeof(string));
                    foreach (DataRow row in dt.Rows)
                    {
                        int q = Convert.ToInt32(row["Quantity"]);
                        row["Display"] = q > 0 ? $"{row["Name"]}  (còn: {q})" : $"{row["Name"]}  ⊘ hết";
                    }

                    cmbBook.DataSource    = dt;
                    cmbBook.DisplayMember = "Display";
                    cmbBook.ValueMember   = "Id";
                }
                UpdateAvail();
            }
            catch (Exception ex) { Err("Lỗi tải sách", ex); }
        }

        void LoadBorrows()
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    var dt = new DataTable();
                    new MySqlDataAdapter(@"
                        SELECT b.Id,
                               u.Name   AS UserName,
                               bk.Name  AS BookName,
                               bd.Quantity AS Qty,
                               DATE_FORMAT(b.BorrowDate,'%d/%m/%Y') AS BorrowDate,
                               b.Status,
                               COALESCE(DATE_FORMAT(b.ReturnDate,'%d/%m/%Y'),'-') AS ReturnDate
                        FROM Borrows b
                        JOIN Users         u  ON b.UserId    = u.Id
                        JOIN BorrowDetails bd ON bd.BorrowId = b.Id
                        JOIN Books         bk ON bk.Id       = bd.BookId
                        ORDER BY b.BorrowDate DESC", conn).Fill(dt);
                    dgv.DataSource = dt;
                }
                ApplyHeaders();
            }
            catch (Exception ex) { Err("Lỗi tải lịch sử", ex); }
        }

        void ApplyHeaders()
        {
            if (dgv.Columns.Count == 0) return;
            SetCol("Id",         "ID",           20);
            SetCol("UserName",   "Người mượn",   90);
            SetCol("BookName",   "Tên sách",      130);
            SetCol("Qty",        "SL",            20);
            SetCol("BorrowDate", "Ngày mượn",     70);
            SetCol("Status",     "Trạng thái",    55);
            SetCol("ReturnDate", "Ngày trả",      60);
        }

        void SetCol(string col, string hdr, int w)
        {
            if (dgv.Columns.Contains(col))
            { dgv.Columns[col].HeaderText = hdr; dgv.Columns[col].FillWeight = w; }
        }

        // ── Clicking a row → fill form (edit mode) ────────────────────────────
        void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            var row = dgv.CurrentRow;
            if (row == null) return;
            try
            {
                int id = Convert.ToInt32(row.Cells["Id"].Value);
                SetEditMode(id, row);
            }
            catch { }
        }

        void SetEditMode(int borrowId, DataGridViewRow row)
        {
            editingBorrowId = borrowId;

            // Try to match User combobox
            string userName = row.Cells["UserName"].Value?.ToString() ?? "";
            foreach (DataRowView drv in (DataTable)cmbUser.DataSource)
                if (drv["Name"].ToString() == userName) { cmbUser.SelectedItem = drv; break; }

            // Try to match Book combobox
            string bookName = row.Cells["BookName"].Value?.ToString() ?? "";
            foreach (DataRowView drv in (DataTable)cmbBook.DataSource)
                if (drv["Name"].ToString() == bookName) { cmbBook.SelectedItem = drv; break; }

            // Quantity
            nudQty.Value = Convert.ToDecimal(row.Cells["Qty"].Value);

            // BorrowDate
            string dateStr = row.Cells["BorrowDate"].Value?.ToString() ?? "";
            if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime dt))
                dtpBorrowDate.Value = dt;

            // Switch UI to edit mode
            lblMode.Text      = $"✎  Đang sửa phiếu #{borrowId}";
            lblMode.ForeColor = Color.FromArgb(52, 152, 219);
            btnSave.Text      = "✎  Lưu thay đổi";
            btnSave.BackColor = Color.FromArgb(52, 152, 219);
            btnDelete.Enabled = true;
        }

        void SetCreateMode()
        {
            editingBorrowId   = -1;
            lblMode.Text      = "✚  Tạo phiếu mượn mới";
            lblMode.ForeColor = Color.FromArgb(39, 174, 96);
            btnSave.Text      = "✚  Tạo phiếu";
            btnSave.BackColor = Color.FromArgb(39, 174, 96);
            btnDelete.Enabled = false;
            dtpBorrowDate.Value = DateTime.Today;
            nudQty.Value      = 1;
            if (cmbUser.Items.Count > 0) cmbUser.SelectedIndex = 0;
            if (cmbBook.Items.Count > 0) cmbBook.SelectedIndex = 0;
            dgv.ClearSelection();
            UpdateAvail();
        }

        // ── Save (Create OR Update) ───────────────────────────────────────────
        void BtnSave_Click(object sender, EventArgs e)
        {
            if (editingBorrowId < 0)
                CreateBorrow();
            else
                UpdateBorrow();
        }

        void CreateBorrow()
        {
            if (!ValidateForm(out int userId, out int bookId, out int qty)) return;

            int stock = GetStock(bookId);
            if (stock <= 0) { Info("Sách này đã hết — không thể tạo phiếu."); return; }
            if (qty > stock) { Info($"Chỉ còn {stock} quyển, không đủ để mượn {qty}."); return; }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            // insert borrow header with chosen date
                            int borrowId;
                            using (var cmd = new MySqlCommand(
                                "INSERT INTO Borrows (UserId,BorrowDate,Status) VALUES (@uid,@d,'borrowing')",
                                conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@uid", userId);
                                cmd.Parameters.AddWithValue("@d",   dtpBorrowDate.Value.Date);
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = new MySqlCommand("SELECT LAST_INSERT_ID()", conn, tx))
                                borrowId = Convert.ToInt32(cmd.ExecuteScalar());

                            // insert detail
                            using (var cmd = new MySqlCommand(
                                "INSERT INTO BorrowDetails (BorrowId,BookId,Quantity) VALUES (@b,@bk,@q)",
                                conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@b",  borrowId);
                                cmd.Parameters.AddWithValue("@bk", bookId);
                                cmd.Parameters.AddWithValue("@q",  qty);
                                cmd.ExecuteNonQuery();
                            }

                            // decrement stock (atomic check)
                            using (var cmd = new MySqlCommand(
                                "UPDATE Books SET Quantity=Quantity-@q WHERE Id=@id AND Quantity>=@q",
                                conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@q",  qty);
                                cmd.Parameters.AddWithValue("@id", bookId);
                                if (cmd.ExecuteNonQuery() == 0)
                                    throw new Exception("Không đủ sách (đã được mượn bởi người khác).");
                            }

                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                Reload();
                Info("Tạo phiếu mượn thành công! ✔");
            }
            catch (Exception ex) { Err("Lỗi tạo phiếu", ex); }
        }

        void UpdateBorrow()
        {
            // Edit: update BorrowDate + Quantity (adjust stock delta)
            if (!ValidateForm(out int userId, out int bookId, out int newQty)) return;

            try
            {
                // Get original detail for this borrow
                int origBookId, origQty;
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "SELECT BookId, Quantity FROM BorrowDetails WHERE BorrowId=@id LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", editingBorrowId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) { Info("Không tìm thấy chi tiết phiếu."); return; }
                            origBookId = r.GetInt32(0);
                            origQty    = r.GetInt32(1);
                        }
                    }
                }

                // Only allow editing same book for Level 1 simplicity
                if (bookId != origBookId)
                { Info("Không thể đổi sách trên phiếu đang mượn.\nXóa phiếu cũ và tạo phiếu mới."); return; }

                int delta = newQty - origQty;   // positive = needs more books; negative = frees books
                if (delta > 0)
                {
                    int freeStock = GetStock(origBookId);
                    if (freeStock < delta)
                    { Info($"Chỉ còn {freeStock} quyển tự do, không đủ để tăng thêm {delta}."); return; }
                }

                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            // update header date & user
                            using (var cmd = new MySqlCommand(
                                "UPDATE Borrows SET UserId=@uid, BorrowDate=@d WHERE Id=@id",
                                conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@uid", userId);
                                cmd.Parameters.AddWithValue("@d",   dtpBorrowDate.Value.Date);
                                cmd.Parameters.AddWithValue("@id",  editingBorrowId);
                                cmd.ExecuteNonQuery();
                            }

                            // update detail quantity
                            using (var cmd = new MySqlCommand(
                                "UPDATE BorrowDetails SET Quantity=@q WHERE BorrowId=@id",
                                conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@q",  newQty);
                                cmd.Parameters.AddWithValue("@id", editingBorrowId);
                                cmd.ExecuteNonQuery();
                            }

                            // adjust book stock (delta can be positive or negative)
                            if (delta != 0)
                                using (var cmd = new MySqlCommand(
                                    "UPDATE Books SET Quantity=Quantity-@d WHERE Id=@id",
                                    conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@d",  delta);
                                    cmd.Parameters.AddWithValue("@id", origBookId);
                                    cmd.ExecuteNonQuery();
                                }

                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                Reload();
                Info("Cập nhật phiếu thành công! ✔");
            }
            catch (Exception ex) { Err("Lỗi cập nhật phiếu", ex); }
        }

        // ── Delete borrow ─────────────────────────────────────────────────────
        void BtnDelete_Click(object sender, EventArgs e)
        {
            if (editingBorrowId < 0) { Info("Chọn một phiếu trong bảng trước."); return; }

            var row = dgv.CurrentRow;
            string status = row?.Cells["Status"].Value?.ToString() ?? "";
            string warn   = status == "borrowing"
                ? "\n⚠ Sách chưa trả — số lượng sẽ được hoàn lại tự động."
                : "";

            if (MessageBox.Show(
                    $"Xóa phiếu #{editingBorrowId}?{warn}",
                    "Xác nhận xóa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            // if still borrowing → restore stock first
                            if (status == "borrowing")
                            {
                                using (var cmd = new MySqlCommand(
                                    "UPDATE Books bk " +
                                    "JOIN BorrowDetails bd ON bd.BookId=bk.Id " +
                                    "SET bk.Quantity=bk.Quantity+bd.Quantity " +
                                    "WHERE bd.BorrowId=@id", conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@id", editingBorrowId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // delete detail first (FK child)
                            using (var cmd = new MySqlCommand(
                                "DELETE FROM BorrowDetails WHERE BorrowId=@id", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@id", editingBorrowId);
                                cmd.ExecuteNonQuery();
                            }

                            // delete header
                            using (var cmd = new MySqlCommand(
                                "DELETE FROM Borrows WHERE Id=@id", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@id", editingBorrowId);
                                cmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                Reload();
                Info("Đã xóa phiếu mượn! ✔");
            }
            catch (Exception ex) { Err("Lỗi xóa phiếu", ex); }
        }

        // ── Return borrow ─────────────────────────────────────────────────────
        void BtnReturn_Click(object sender, EventArgs e)
        {
            var row = dgv.CurrentRow;
            if (row == null) { Info("Chọn một phiếu mượn trong bảng."); return; }

            string status = row.Cells["Status"].Value?.ToString() ?? "";
            if (status != "borrowing") { Info("Phiếu này đã được trả rồi."); return; }

            int id    = Convert.ToInt32(row.Cells["Id"].Value);
            string who  = row.Cells["UserName"].Value?.ToString() ?? "";
            string book = row.Cells["BookName"].Value?.ToString()  ?? "";

            if (MessageBox.Show(
                    $"Xác nhận trả sách?\n\nNgười mượn : {who}\nSách       : {book}",
                    "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            // restore stock
                            using (var cmd = new MySqlCommand(
                                "UPDATE Books bk " +
                                "JOIN BorrowDetails bd ON bd.BookId=bk.Id " +
                                "SET bk.Quantity=bk.Quantity+bd.Quantity " +
                                "WHERE bd.BorrowId=@id", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }

                            // mark returned
                            using (var cmd = new MySqlCommand(
                                "UPDATE Borrows SET Status='returned',ReturnDate=NOW() WHERE Id=@id",
                                conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                Reload();
                Info("Trả sách thành công! ✔");
            }
            catch (Exception ex) { Err("Lỗi trả sách", ex); }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        bool ValidateForm(out int userId, out int bookId, out int qty)
        {
            userId = bookId = qty = 0;

            if (cmbUser.DataSource == null || ((DataTable)cmbUser.DataSource).Rows.Count == 0)
            { Info("Chưa có người dùng. Vào tab Users để thêm trước."); return false; }
            if (cmbBook.DataSource == null || ((DataTable)cmbBook.DataSource).Rows.Count == 0)
            { Info("Chưa có sách. Vào tab Books để thêm trước."); return false; }
            if (cmbUser.SelectedValue == null) { Info("Vui lòng chọn người dùng."); return false; }
            if (cmbBook.SelectedValue == null) { Info("Vui lòng chọn sách.");       return false; }

            userId = Convert.ToInt32(cmbUser.SelectedValue);
            bookId = Convert.ToInt32(cmbBook.SelectedValue);
            qty    = (int)nudQty.Value;
            return true;
        }

        void UpdateAvail()
        {
            var row = cmbBook.SelectedItem as DataRowView;
            if (row == null) { lblAvail.Text = ""; return; }
            int q = Convert.ToInt32(row["Quantity"]);
            lblAvail.Text      = q > 0 ? $"{q} quyển" : "Hết sách";
            lblAvail.ForeColor = q > 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);
            nudQty.Maximum     = q > 0 ? q : 1;
        }

        int GetStock(int bookId)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT Quantity FROM Books WHERE Id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", bookId);
                    var r = cmd.ExecuteScalar();
                    return r == null ? 0 : Convert.ToInt32(r);
                }
            }
        }

        void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (!dgv.Columns.Contains("Status") || e.Value == null) return;
            if (e.ColumnIndex != dgv.Columns["Status"].Index) return;
            bool borrowing = e.Value.ToString() == "borrowing";
            e.CellStyle.ForeColor   = borrowing ? Color.FromArgb(180, 60, 0) : Color.FromArgb(30, 130, 76);
            e.CellStyle.Font        = new Font("Segoe UI", 9f, FontStyle.Bold);
            e.FormattingApplied     = true;
        }

        static Label Lbl(string text) => new Label
        {
            Text = text, TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(80, 100, 130), Padding = new Padding(0, 0, 8, 0)
        };

        static Button Btn(string text, Color bg)
        {
            var b = new Button
            {
                Text = text, BackColor = bg, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f),
                Size = new Size(120, 34), Margin = new Padding(0, 0, 8, 0), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        static void Info(string msg) => MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        static void Err(string ctx, Exception ex) => MessageBox.Show(ctx + ":\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
