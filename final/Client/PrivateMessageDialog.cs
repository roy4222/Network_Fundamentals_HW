using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChatClient
{
    /// <summary>
    /// 私訊輸入對話框
    /// 用於替代 VisualBasic.InputBox，提供更好的使用者體驗
    /// </summary>
    public partial class PrivateMessageDialog : Form
    {
        private TextBox _messageTextBox = null!;
        private Button _sendButton = null!;
        private Button _cancelButton = null!;
        private Label _promptLabel = null!;

        public string Message { get; private set; } = string.Empty;
        public DialogResult Result { get; private set; }

        public PrivateMessageDialog(string targetUser)
        {
            InitializeComponent(targetUser);
        }

        /// <summary>
        /// 顯示私訊輸入對話框
        /// </summary>
        /// <param name="targetUser">目標使用者名稱</param>
        /// <returns>使用者輸入的訊息，如果取消則返回 null</returns>
        public static string? ShowDialog(string targetUser)
        {
            using var dialog = new PrivateMessageDialog(targetUser);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.Message;
            }
            return null;
        }

        private void InitializeComponent(string targetUser)
        {
            this.SuspendLayout();

            // 對話框設定
            this.Text = $"發送私訊給 {targetUser}";
            this.Size = new Size(400, 180);
            this.MinimumSize = new Size(350, 150);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // 提示標籤
            _promptLabel = new Label
            {
                Text = $"請輸入要發送給 {targetUser} 的私訊:",
                Location = new Point(15, 15),
                Size = new Size(350, 20),
                Font = new Font("微軟正黑體", 9F)
            };
            this.Controls.Add(_promptLabel);

            // 訊息輸入框
            _messageTextBox = new TextBox
            {
                Location = new Point(15, 45),
                Size = new Size(350, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("微軟正黑體", 9F),
                TabIndex = 0
            };
            this.Controls.Add(_messageTextBox);

            // 發送按鈕
            _sendButton = new Button
            {
                Text = "發送",
                Location = new Point(200, 115),
                Size = new Size(80, 25),
                BackColor = Color.LightBlue,
                TabIndex = 1,
                DialogResult = DialogResult.OK
            };
            _sendButton.Click += OnSendClick;
            this.Controls.Add(_sendButton);

            // 取消按鈕
            _cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(290, 115),
                Size = new Size(80, 25),
                BackColor = Color.LightGray,
                TabIndex = 2,
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(_cancelButton);

            // 設定預設按鈕
            this.AcceptButton = _sendButton;
            this.CancelButton = _cancelButton;

            // 焦點設定
            _messageTextBox.Focus();

            this.ResumeLayout(false);
        }

        private void OnSendClick(object? sender, EventArgs e)
        {
            Message = _messageTextBox.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(Message))
            {
                MessageBox.Show("訊息內容不能為空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _messageTextBox.Focus();
                return;
            }

            Result = DialogResult.OK;
            this.Close();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _messageTextBox.Focus();
        }
    }
} 