using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Library
{
    public class UsersPanel : Panel
    {
        TextBox txtName, txtPhone, txtEmail;
        DataGridView dgv;
        int selectedId = -1;

        public UsersPanel()
        {
            BackColor = Color.FromArgb(242, 246, 251);
            Padding   = new Padding(24, 20, 24, 16);
            Build();
            LoadData();
        }

        void Build()
        {
            var title = new Label
            {
                Text      = "User Management",
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
                Height    = 92,
                BackColor = Color.White,
                Padding   = new Padding(16, 10, 16, 10)
            };
            card.Paint += BooksPanel.CardBorder2;

            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 6,
                RowCount    = 1
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            txtName  = MakeTxt();
            txtPhone = MakeTxt();
            txtEmail = MakeTxt();

            tbl.Controls.Add(Lbl("Name:"),  0, 0); tbl.Controls.Add(txtName,  1, 0);
            tbl.Controls.Add(Lbl("Phone:"), 2, 0); tbl.Controls.Add(txtPhone, 3, 0);
            tbl.Controls.Add(Lbl("Email:"), 4, 0); tbl.Controls.Add(txtEmail, 5, 0);
            card.Controls.Add(tbl);

            // ---- button bar ----
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

            dgv = new DataGridView { Dock = DockStyle.Fill };
            BooksPanel.StyleGrid(dgv);
            dgv.SelectionChanged += Dgv_SelectionChanged;

            var gap = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };

            Controls.Add(dgv);
            Controls.Add(gap);
            Controls.Add(btnBar);
            Controls.Add(card);
            Controls.Add(title);
        }

        public void LoadData()
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    var dt = new DataTable();
                    new MySqlDataAdapter(
                        "SELECT Id, Name, Phone, Email FROM Users ORDER BY Name",
                        conn).Fill(dt);
                    dgv.DataSource = dt;
                }
                ApplyHeaders();
            }
            catch (Exception ex) { Err("Error loading users", ex); }
        }

        void ApplyHeaders()
        {
            if (dgv.Columns.Count == 0) return;
            SetCol("Id",    "ID",    25);
            SetCol("Name",  "Name",  120);
            SetCol("Phone", "Phone", 60);
            SetCol("Email", "Email", 100);
        }

        void SetCol(string col, string hdr, int weight)
        {
            if (dgv.Columns.Contains(col))
            {
                dgv.Columns[col].HeaderText = hdr;
                dgv.Columns[col].FillWeight = weight;
            }
        }

        void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            var row = dgv.CurrentRow;
            if (row == null) return;
            try
            {
                selectedId     = Convert.ToInt32(row.Cells["Id"].Value);
                txtName.Text   = row.Cells["Name"].Value?.ToString()  ?? "";
                txtPhone.Text  = row.Cells["Phone"].Value?.ToString() ?? "";
                txtEmail.Text  = row.Cells["Email"].Value?.ToString() ?? "";
            }
            catch { }
        }

        void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            string dupErr = CheckContactDuplicate(txtPhone.Text.Trim(), txtEmail.Text.Trim());
            if (dupErr != null) { Info(dupErr); return; }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO Users (Name,Phone,Email) VALUES (@n,@p,@em)", conn))
                    {
                        cmd.Parameters.AddWithValue("@n",  txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@p",  txtPhone.Text.Trim());
                        cmd.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }
                ClearForm(); LoadData();
            }
            catch (Exception ex) { Err("Lỗi thêm người dùng", ex); }
        }

        void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedId < 0) { Info("Chọn một người dùng trước."); return; }
            if (!ValidateInput()) return;

            string dupErr = CheckContactDuplicate(txtPhone.Text.Trim(), txtEmail.Text.Trim(), selectedId);
            if (dupErr != null) { Info(dupErr); return; }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "UPDATE Users SET Name=@n,Phone=@p,Email=@em WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@n",  txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@p",  txtPhone.Text.Trim());
                        cmd.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                ClearForm(); LoadData();
            }
            catch (Exception ex) { Err("Lỗi cập nhật người dùng", ex); }
        }

        void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedId < 0) { Info("Chọn một người dùng trước."); return; }

            int active = ActiveBorrows(selectedId);
            if (active > 0)
            {
                Info($"Không thể xóa: người dùng này đang có {active} phiếu mượn chưa trả.\n" +
                     "Vui lòng trả sách trước rồi mới xóa."); return;
            }
            if (MessageBox.Show("Xóa người dùng này?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("DELETE FROM Users WHERE Id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                ClearForm(); LoadData();
            }
            catch (Exception ex) { Err("Lỗi xóa người dùng", ex); }
        }

        bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                Info("Tên không được để trống."); txtName.Focus(); return false;
            }
            // Phone: digits only, 10-11 chars (if provided)
            var phone = txtPhone.Text.Trim();
            if (phone.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\d{10,11}$"))
            {
                Info("Số điện thoại không hợp lệ (10-11 chữ số)."); txtPhone.Focus(); return false;
            }
            // Email: must contain @ and . (if provided)
            var email = txtEmail.Text.Trim();
            if (email.Length > 0 && (!email.Contains("@") || email.IndexOf('.', email.IndexOf('@')) < 0))
            {
                Info("Email không hợp lệ."); txtEmail.Focus(); return false;
            }
            return true;
        }

        // Returns error message if phone/email already belongs to another user, else null
        string CheckContactDuplicate(string phone, string email, int excludeId = -1)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                if (phone.Length > 0)
                {
                    using (var cmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM Users WHERE Phone=@p AND Phone<>'' AND Id<>@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@p",  phone);
                        cmd.Parameters.AddWithValue("@id", excludeId);
                        if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                            return "Số điện thoại này đã được đăng ký bởi người dùng khác.";
                    }
                }
                if (email.Length > 0)
                {
                    using (var cmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM Users WHERE Email=@em AND Email<>'' AND Id<>@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@em", email);
                        cmd.Parameters.AddWithValue("@id", excludeId);
                        if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                            return "Email này đã được đăng ký bởi người dùng khác.";
                    }
                }
                return null;
            }
        }

        // Returns number of active (not yet returned) borrows for this user
        int ActiveBorrows(int userId)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM Borrows WHERE UserId=@id AND Status='borrowing'", conn))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        void ClearForm()
        {
            selectedId    = -1;
            txtName.Text  = "";
            txtPhone.Text = "";
            txtEmail.Text = "";
            dgv.ClearSelection();
        }

        static TextBox MakeTxt() => new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10f) };

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

        static void Info(string msg) => MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        static void Err(string ctx, Exception ex) => MessageBox.Show(ctx + ":\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
