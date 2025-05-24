using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing;

namespace TopUpTerminal
{
    public class MobileOperator
    {
        public string Name { get; set; }
        public List<string> Prefixes { get; set; }
    }

    public class BankCard
    {
        public string Number { get; set; }
        public string Expiry { get; set; }
        public string CVV { get; set; }
        public string Pin { get; set; }
        public decimal Balance { get; set; }
    }

    public partial class MainForm : Form
    {
        private Dictionary<string, MobileOperator> operators = new Dictionary<string, MobileOperator>();
        private Dictionary<string, List<BankCard>> bankCards = new Dictionary<string, List<BankCard>>();

        private string currentLanguage = "UA"; // UA or EN
        private decimal commissionPercent = 2.0m; // комісія банку 2%

        ComboBox comboLanguage = new ComboBox();
        ComboBox comboOperator = new ComboBox();
        TextBox txtPhone = new TextBox();
        TextBox txtCardNumber = new TextBox();
        TextBox txtExpiry = new TextBox(); // Змінено з MaskedTextBox на TextBox
        TextBox txtCVV = new TextBox();
        TextBox txtPin = new TextBox();
        NumericUpDown numAmount = new NumericUpDown();
        NumericUpDown numDonation = new NumericUpDown();
        CheckBox chkReceipt = new CheckBox();
        Button btnTopUp = new Button();
        Label lblCommission = new Label();

        public MainForm()
        {
            InitializeComponent();
            LoadOperators();
            LoadBankCards();
            SetupUI();
            ApplyLanguage();
        }

        private void InitializeComponent()
        {
            this.Text = "Термінал поповнення мобільного рахунку / Mobile Top-Up Terminal";
            this.Width = 520;
            this.Height = 440;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            comboLanguage.Items.AddRange(new string[] { "Українська", "English" });
            comboLanguage.SelectedIndex = 0;
            comboLanguage.Location = new Point(20, 20);
            comboLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            comboLanguage.SelectedIndexChanged += (s, e) =>
            {
                currentLanguage = comboLanguage.SelectedIndex == 0 ? "UA" : "EN";
                ApplyLanguage();
                LogAction($"Language changed to {(currentLanguage == "UA" ? "Ukrainian" : "English")}");
            };
            this.Controls.Add(comboLanguage);

            comboOperator.Location = new Point(20, 70);
            comboOperator.DropDownStyle = ComboBoxStyle.DropDownList;
            this.Controls.Add(comboOperator);

            txtPhone.Location = new Point(20, 120);
            txtPhone.Width = 200;
            this.Controls.Add(txtPhone);

            txtCardNumber.Location = new Point(20, 170);
            txtCardNumber.Width = 200;
            this.Controls.Add(txtCardNumber);

            // Оновлений блок для txtExpiry
            txtExpiry.Location = new Point(250, 170);
            txtExpiry.Width = 60;
            txtExpiry.MaxLength = 5;
            txtExpiry.KeyPress += TxtExpiry_KeyPress;
            this.Controls.Add(txtExpiry);

            txtCVV.Location = new Point(320, 170);
            txtCVV.Width = 50;
            txtCVV.PasswordChar = '*';
            this.Controls.Add(txtCVV);

            txtPin.Location = new Point(20, 220);
            txtPin.Width = 100;
            txtPin.PasswordChar = '*';
            this.Controls.Add(txtPin);

            numAmount.Location = new Point(20, 270);
            numAmount.Minimum = 1;
            numAmount.Maximum = 10000;
            numAmount.DecimalPlaces = 2;
            numAmount.Increment = 1;
            this.Controls.Add(numAmount);

            numDonation.Location = new Point(150, 270);
            numDonation.Minimum = 0;
            numDonation.Maximum = 100;
            numDonation.DecimalPlaces = 0;
            this.Controls.Add(numDonation);

            lblCommission.Location = new Point(20, 310);
            lblCommission.Width = 400;
            this.Controls.Add(lblCommission);

            chkReceipt.Location = new Point(20, 340);
            this.Controls.Add(chkReceipt);

            btnTopUp.Text = "Поповнити / Top Up";
            btnTopUp.Location = new Point(20, 380);
            btnTopUp.Click += BtnTopUp_Click;
            this.Controls.Add(btnTopUp);
        }

        private void TxtExpiry_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                return;
            }

            var textBox = (TextBox)sender;
            string text = textBox.Text;

            // Автоматично додаємо слеш після 2 цифр
            if (text.Length == 2 && !text.Contains('/'))
            {
                textBox.Text = text + "/";
                textBox.SelectionStart = 3;
            }
        }


        private void SetupUI()
        {
            comboOperator.Items.Clear();
            foreach (var op in operators.Values)
                comboOperator.Items.Add(op.Name);

            if (comboOperator.Items.Count > 0)
                comboOperator.SelectedIndex = 0;

            chkReceipt.Checked = false;
        }

        private void ApplyLanguage()
        {
            if (currentLanguage == "UA")
            {
                this.Text = "Термінал поповнення мобільного рахунку";

                comboOperator.Text = "Оператор";
                lblCommission.Text = $"Комісія банку: {commissionPercent}%";
                chkReceipt.Text = "Друкувати чек";
                btnTopUp.Text = "Поповнити";

                txtPhone.Text = "Номер телефону";
                txtPhone.ForeColor = Color.Gray;
                txtCardNumber.Text = "Номер картки";
                txtCardNumber.ForeColor = Color.Gray;
                txtExpiry.Text = "ММ/РР";
                txtExpiry.ForeColor = Color.Gray;
                txtCVV.Text = "CVV";
                txtCVV.ForeColor = Color.Gray;
                txtPin.Text = "PIN-код";
                txtPin.ForeColor = Color.Gray;
            }
            else
            {
                this.Text = "Mobile Top-Up Terminal";

                comboOperator.Text = "Operator";
                lblCommission.Text = $"Bank commission: {commissionPercent}%";
                chkReceipt.Text = "Print receipt";
                btnTopUp.Text = "Top Up";

                txtPhone.Text = "Phone number";
                txtPhone.ForeColor = Color.Gray;
                txtCardNumber.Text = "Card number";
                txtCardNumber.ForeColor = Color.Gray;
                txtExpiry.Text = "MM/YY";
                txtExpiry.ForeColor = Color.Gray;
                txtCVV.Text = "CVV";
                txtCVV.ForeColor = Color.Gray;
                txtPin.Text = "PIN code";
                txtPin.ForeColor = Color.Gray;
            }
        }

        private void LoadOperators()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "operators.txt");
                if (!File.Exists(path))
                {
                    MessageBox.Show($"File {path} not found!");
                    return;
                }

                operators.Clear();

                foreach (var line in File.ReadLines(path))
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        var opName = parts[0].Trim();
                        var prefixes = parts[1].Split(',')
                            .Where(p => !string.IsNullOrWhiteSpace(p))
                            .Select(p => p.Trim())
                            .ToList();

                        operators[opName] = new MobileOperator() { Name = opName, Prefixes = prefixes };
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading operators: " + ex.Message);
            }
        }

        private void LoadBankCards()
        {
            try
            {
                string banksDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "banks");
                if (!Directory.Exists(banksDir))
                {
                    MessageBox.Show($"Directory {banksDir} not found!");
                    return;
                }

                bankCards.Clear();

                foreach (var file in Directory.GetFiles(banksDir, "*.json"))
                {
                    string bankName = Path.GetFileNameWithoutExtension(file);
                    string json = File.ReadAllText(file);
                    var cards = JsonSerializer.Deserialize<List<BankCard>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (cards != null)
                        bankCards[bankName] = cards;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading bank cards: " + ex.Message);
            }
        }

        private void BtnTopUp_Click(object sender, EventArgs e)
        {
            try
            {
                // Валідація вводу
                string phone = txtPhone.Text.Trim();
                string cardNumber = txtCardNumber.Text.Trim().Replace(" ", "");
                string expiry = txtExpiry.Text.Trim();
                string cvv = txtCVV.Text.Trim();
                string pin = txtPin.Text.Trim();
                decimal amount = numAmount.Value;
                decimal donation = numDonation.Value;
                bool wantReceipt = chkReceipt.Checked;
                string operatorName = comboOperator.SelectedItem?.ToString();

                if (!ValidatePhone(phone))
                {
                    ShowMessage(currentLanguage == "UA" ? "Неправильний номер телефону" : "Invalid phone number");
                    return;
                }
                if (!ValidateCardNumber(cardNumber))
                {
                    ShowMessage(currentLanguage == "UA" ? "Неправильний номер картки" : "Invalid card number");
                    return;
                }
                if (!ValidateExpiry(expiry))
                {
                    ShowMessage(currentLanguage == "UA" ? "Неправильний термін дії картки" : "Invalid card expiry date");
                    return;
                }
                if (!ValidateCVV(cvv))
                {
                    ShowMessage(currentLanguage == "UA" ? "Неправильний CVV" : "Invalid CVV");
                    return;
                }
                if (!ValidatePin(pin))
                {
                    ShowMessage(currentLanguage == "UA" ? "Неправильний PIN" : "Invalid PIN");
                    return;
                }
                if (amount <= 0)
                {
                    ShowMessage(currentLanguage == "UA" ? "Сума поповнення має бути більше 0" : "Top-up amount must be greater than 0");
                    return;
                }
                if (donation < 0)
                {
                    ShowMessage(currentLanguage == "UA" ? "Пожертва не може бути від'ємною" : "Donation cannot be negative");
                    return;
                }
                if (operatorName == null || !operators.ContainsKey(operatorName))
                {
                    ShowMessage(currentLanguage == "UA" ? "Оберіть оператора" : "Please select operator");
                    return;
                }
                if (!CheckPhonePrefix(phone, operators[operatorName]))
                {
                    ShowMessage(currentLanguage == "UA" ? "Номер телефону не належить обраному оператору" : "Phone number does not match selected operator");
                    return;
                }

                // Нормалізація формату дати
                expiry = NormalizeExpiry(expiry);

                // Знаходження картки
                BankCard card = FindCard(cardNumber, expiry, cvv, pin);
                if (card == null)
                {
                    ShowMessage(currentLanguage == "UA" ? "Картка не знайдена або неправильні дані" : "Card not found or invalid data");
                    return;
                }

                decimal commission = Math.Round(amount * commissionPercent / 100, 2);
                decimal totalCharge = amount + commission;

                if (card.Balance < totalCharge)
                {
                    ShowMessage(currentLanguage == "UA" ? "Недостатньо коштів на картці" : "Insufficient card balance");
                    return;
                }

                // Знімаємо кошти
                card.Balance -= totalCharge;

                // Зберігаємо оновлення
                SaveBankCards();

                // Логування
                LogAction($"Top-up success: Phone {phone}, Operator {operatorName}, Amount {amount}, Donation {donation}, Commission {commission}, Card ****{card.Number.Substring(card.Number.Length - 4)}");

                ShowMessage(currentLanguage == "UA" ? "Поповнення успішне!" : "Top-up successful!");

                // Створення чеку
                if (wantReceipt)
                {
                    CreateReceipt(phone, operatorName, amount, donation, commission, card);
                }

                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private string NormalizeExpiry(string expiry)
        {
            expiry = expiry.Replace(" ", "").Replace("/", "");
            if (expiry.Length == 4)
                return expiry.Insert(2, "/");
            return expiry;
        }

        private bool ValidatePhone(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return false;
            return Regex.IsMatch(phone, @"^\d{9,12}$");
        }

        private bool ValidateCardNumber(string number)
        {
            if (string.IsNullOrEmpty(number)) return false;
            return Regex.IsMatch(number, @"^\d{16}$");
        }

        private bool ValidateExpiry(string expiry)
        {
            if (string.IsNullOrEmpty(expiry)) return false;

            expiry = NormalizeExpiry(expiry);
            if (expiry.Length != 5 || expiry[2] != '/') return false;

            try
            {
                var parts = expiry.Split('/');
                int month = int.Parse(parts[0]);
                int year = 2000 + int.Parse(parts[1]);

                if (month < 1 || month > 12) return false;

                var expiryDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                return expiryDate >= DateTime.Today;
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateCVV(string cvv)
        {
            if (string.IsNullOrEmpty(cvv)) return false;
            return Regex.IsMatch(cvv, @"^\d{3}$");
        }

        private bool ValidatePin(string pin)
        {
            if (string.IsNullOrEmpty(pin)) return false;
            return Regex.IsMatch(pin, @"^\d{4}$");
        }

        private bool CheckPhonePrefix(string phone, MobileOperator op)
        {
            foreach (var prefix in op.Prefixes)
            {
                if (phone.StartsWith(prefix))
                    return true;
            }
            return false;
        }

        private BankCard FindCard(string number, string expiry, string cvv, string pin)
        {
            expiry = NormalizeExpiry(expiry);

            foreach (var bank in bankCards.Values)
            {
                var card = bank.FirstOrDefault(c =>
                    c.Number == number &&
                    c.Expiry == expiry &&
                    c.CVV == cvv &&
                    c.Pin == pin);

                if (card != null)
                    return card;
            }
            return null;
        }

        private void SaveBankCards()
        {
            try
            {
                string banksDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "banks");

                foreach (var kvp in bankCards)
                {
                    string bankName = kvp.Key;
                    var cards = kvp.Value;

                    string filePath = Path.Combine(banksDir, bankName + ".json");
                    string json = JsonSerializer.Serialize(cards, new JsonSerializerOptions { WriteIndented = true });

                    File.WriteAllText(filePath, json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving bank cards: " + ex.Message);
            }
        }

        private void CreateReceipt(string phone, string operatorName, decimal amount, decimal donation, decimal commission, BankCard card)
        {
            try
            {
                string receiptsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Receipts");
                if (!Directory.Exists(receiptsDir))
                    Directory.CreateDirectory(receiptsDir);

                string receiptFile = Path.Combine(receiptsDir, $"Receipt_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                using (var sw = new StreamWriter(receiptFile))
                {
                    sw.WriteLine("=== Mobile Top-Up Receipt ===");
                    sw.WriteLine($"Date: {DateTime.Now}");
                    sw.WriteLine($"Phone: {phone}");
                    sw.WriteLine($"Operator: {operatorName}");
                    sw.WriteLine($"Top-up amount: {amount:C}");
                    sw.WriteLine($"Donation: {donation:C}");
                    sw.WriteLine($"Commission: {commission:C}");
                    sw.WriteLine($"Total charged: {(amount + commission):C}");
                    sw.WriteLine($"Card Number: **** **** **** {card.Number.Substring(card.Number.Length - 4)}");
                    sw.WriteLine("Thank you for using our service!");
                }

                ShowMessage(currentLanguage == "UA" ? $"Чек збережено: {receiptFile}" : $"Receipt saved: {receiptFile}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating receipt: " + ex.Message);
            }
        }

        private void ShowMessage(string message)
        {
            MessageBox.Show(message, currentLanguage == "UA" ? "Повідомлення" : "Message");
        }

        private void LogAction(string message)
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                string logFile = Path.Combine(logDir, "actions.log");
                string logEntry = $"{DateTime.Now}: {message}";
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
            catch
            {
                // Логування не є критичним - ігноруємо помилки
            }
        }

        private void ClearForm()
        {
            txtPhone.Text = currentLanguage == "UA" ? "Номер телефону" : "Phone number";
            txtPhone.ForeColor = Color.Gray;
            txtCardNumber.Text = currentLanguage == "UA" ? "Номер картки" : "Card number";
            txtCardNumber.ForeColor = Color.Gray;
            txtExpiry.Text = currentLanguage == "UA" ? "ММ/РР" : "MM/YY";
            txtExpiry.ForeColor = Color.Gray;
            txtCVV.Text = "CVV";
            txtCVV.ForeColor = Color.Gray;
            txtPin.Text = currentLanguage == "UA" ? "PIN-код" : "PIN code";
            txtPin.ForeColor = Color.Gray;
            numAmount.Value = numAmount.Minimum;
            numDonation.Value = 0;
            chkReceipt.Checked = false;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}