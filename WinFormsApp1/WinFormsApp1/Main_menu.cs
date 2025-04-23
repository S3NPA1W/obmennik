using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;
using System.Data;

namespace WinFormsApp1
{
    public partial class Main_menu : Form
    {
        public int employee;

        static String ConnectionString = "Host = 91.233.173.91; Username = selectel; Password = selectel; Database = selectel";
        private Panel currentContentPanel;
        NpgsqlConnection npgsqlConnection = new NpgsqlConnection(ConnectionString);

        /* Блок функций -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------Блок функций */


        // Сохранение транзакций
        private void SaveTransactionToDatabase(string exchangeably_currency, string received_currency,
                                             decimal exchangeably_count, decimal received_count,
                                             float rate, int employee)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    // Получаем ID для обмениваемой валюты
                    int exchangeably_currency_id = 0;
                    using (var command = new NpgsqlCommand(
                        "SELECT id FROM obmennik.currencies WHERE currency = @currency", connection))
                    {
                        command.Parameters.AddWithValue("@currency", exchangeably_currency);
                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            exchangeably_currency_id = Convert.ToInt32(result);
                        }
                    }

                    // Получаем ID для получаемой валюты
                    int received_currency_id = 0;
                    using (var command = new NpgsqlCommand(
                        "SELECT id FROM obmennik.currencies WHERE currency = @currency", connection))
                    {
                        command.Parameters.AddWithValue("@currency", received_currency);
                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            received_currency_id = Convert.ToInt32(result);
                        }
                    }

                    // Проверяем, что обе валюты найдены
                    if (exchangeably_currency_id == 0 || received_currency_id == 0)
                    {
                        throw new Exception("Не удалось найти ID для одной из валют");
                    }

                    // Вставляем транзакцию
                    using (var command = new NpgsqlCommand(
                        "INSERT INTO obmennik.transactions " +
                        "(exchangeable_currency, received_currency, exchangeable_count, " +
                        "received_count, rate, employee, transaction_date) " +
                        "VALUES (@exchangeably_currency, @received_currency, @exchangeably_count, " +
                        "@received_count, @rate, @employee, @transaction_date)", connection))
                    {
                        command.Parameters.AddWithValue("@exchangeably_currency", exchangeably_currency_id);
                        command.Parameters.AddWithValue("@received_currency", received_currency_id);
                        command.Parameters.AddWithValue("@exchangeably_count", exchangeably_count);
                        command.Parameters.AddWithValue("@received_count", received_count);
                        command.Parameters.AddWithValue("@rate", rate);
                        command.Parameters.AddWithValue("@employee", employee);
                        command.Parameters.AddWithValue("@transaction_date", DateTime.Now);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки или обработка исключения
                MessageBox.Show($"Ошибка при сохранении транзакции: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw; // Можно либо обработать, либо пробросить дальше
            }
        }


        // Функция которая создаёт строку с курсом рубля к валюте
        public string Give_Rate_for_Ruble(string currency)
        {
            string sign = "";
            string rate = "";
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand("SELECT c.rate FROM obmennik.currencies c WHERE c.currency = @currency", connection))
                {
                    command.Parameters.AddWithValue("@currency", currency);
                    Decimal rates = Convert.ToDecimal(command.ExecuteScalar());
                    rate = rates.ToString();
                    using (var command1 = new NpgsqlCommand("SELECT c.sign FROM  obmennik.currencies c WHERE c.currency = @currency", connection))
                    {
                        command1.Parameters.AddWithValue("@currency", currency);
                        sign = Convert.ToString(command1.ExecuteScalar());
                    }
                    return rate + " --> " + sign;
                }
            }
        }


        // функция которая указывает баланс валюты у обменника
        public decimal Give_Balance(string currency)
        {
            decimal balance = 0.00m;
            using (var connection = new NpgsqlConnection(ConnectionString))
            {

                connection.Open();
                var command = new NpgsqlCommand("SELECT c.count FROM obmennik.currencies c WHERE c.currency = @a", connection);
                command.Parameters.AddWithValue("@a", currency);
                balance = Convert.ToDecimal(command.ExecuteScalar());
                return balance;
            }
        }
        // функция которая указывает курс валюты у обменника
        public decimal Give_Rate(string currency)
        {
            decimal balance = 0.00m;
            using (var connection = new NpgsqlConnection(ConnectionString))
            {

                connection.Open();
                var command = new NpgsqlCommand("SELECT c.rate FROM obmennik.currencies c WHERE c.currency = @a", connection);
                command.Parameters.AddWithValue("@a", currency);
                balance = Convert.ToDecimal(command.ExecuteScalar());
                return balance;
            }
        }


        // функция которая изменяет баланс валюты у обменника
        public void Set_Balance(string currency, decimal balance)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new NpgsqlCommand("UPDATE obmennik.currencies SET count = @nb WHERE currency = @a", connection);
                command.Parameters.AddWithValue("@a", currency);
                command.Parameters.AddWithValue("@nb", balance);
                command.ExecuteNonQuery(); // Эта строка была пропущена
            }
        }

        // функция для заполнения таблицы
        private void LoadTransactionHistory(DataGridView gridView)
        {
            try
            {
                npgsqlConnection.Open();
                NpgsqlCommand command = new NpgsqlCommand(
                    @"SELECT 
                t.id as ""ID"",
                t.transaction_date as ""Дата"",
                e.fio as ""Работник"",
                ec.currency as ""Обмениваемая"",
                t.exchangeable_count as ""Сумма"",
                rc.currency as ""Полученная"",
                t.received_count as ""Сумма_полученная""
              FROM obmennik.transactions t
              JOIN obmennik.employees e ON t.employee = e.id
              JOIN obmennik.currencies ec ON t.exchangeable_currency = ec.id
              JOIN obmennik.currencies rc ON t.received_currency = rc.id
              ORDER BY t.transaction_date DESC",
                    npgsqlConnection);

                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command);
                DataTable db = new DataTable();
                adapter.Fill(db);

                gridView.DataSource = db;

                // Настройка форматирования столбцов
                if (gridView.Columns.Contains("Сумма"))
                {
                    gridView.Columns["Сумма"].DefaultCellStyle.Format = "N2";
                    gridView.Columns["Сумма"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                if (gridView.Columns.Contains("Сумма_полученная"))
                {
                    gridView.Columns["Сумма_полученная"].DefaultCellStyle.Format = "N2";
                    gridView.Columns["Сумма_полученная"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                if (gridView.Columns.Contains("Дата"))
                {
                    gridView.Columns["Дата"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm";
                }

                // Скрываем колонку ID
                if (gridView.Columns.Contains("ID"))
                {
                    gridView.Columns["ID"].Visible = false;
                }
            }
            finally
            {
                if (npgsqlConnection.State == ConnectionState.Open)
                    npgsqlConnection.Close();
            }
        }

        // Функция для удаления транзакции
        private void DeleteTransaction(int transactionId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand("DELETE FROM obmennik.transactions WHERE id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", transactionId);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Транзакция успешно удалена", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Транзакция не найдена", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении транзакции: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            string menuItem = clickedButton.Tag.ToString();

            currentContentPanel.Controls.Clear();

            switch (menuItem)
            {
                case "Курсы валют":
                    ShowRatesContent();
                    break;
                case "Обмен валют":
                    ShowExchangeContent();
                    break;
                case "История операций":
                    ShowTransactionHistory();
                    break;
            }
        }

        /* конец блока функций ----------------------------------------------------------------------------------------------------------------------------------------------------------------------конец блока функций */

        public Main_menu()
        {
            InitializeComponent();
            InitializeMainForm();
        }

        private void InitializeMainForm()
        {
            // Настройки главной формы
            this.Text = "Bushido Bucks - Обмен валют";
            this.ClientSize = new Size(800, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(25, 25, 30);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Логотип
            PictureBox logo = new PictureBox
            {
                Size = new Size(150, 150),
                Location = new Point(5, 5),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = Properties.Resources.logo
            };
            this.Controls.Add(logo);

            // Боковое меню из кнопок
            Panel sideMenuPanel = new Panel
            {
                Width = 150,
                Height = 400,
                Location = new Point(0, 170),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom,
                BackColor = Color.FromArgb(40, 40, 45)
            };
            this.Controls.Add(sideMenuPanel);

            // Кнопки меню
            string[] menuItems = { "Курсы валют", "Обмен валют", "История операций" };
            int yPos = 20;

            foreach (var menuItem in menuItems)
            {
                Button menuButton = new Button
                {
                    Text = menuItem,
                    Size = new Size(130, 50),
                    Location = new Point(10, yPos),
                    BackColor = Color.FromArgb(60, 60, 70),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Tag = menuItem,
                    Font = new Font("Segoe UI", 10)
                };
                menuButton.FlatAppearance.BorderSize = 0;
                menuButton.Click += MenuButton_Click;

                sideMenuPanel.Controls.Add(menuButton);
                yPos += 60;
            }

            // Панель для контента
            currentContentPanel = new Panel
            {
                Height = 400,
                Width = 650,
                Location = new Point(150, 0),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = Color.FromArgb(25, 25, 30),
                BorderStyle = BorderStyle.None
            };
            this.Controls.Add(currentContentPanel);

            // Автоматическое изменение размеров
            this.SizeChanged += (s, e) =>
            {
                currentContentPanel.Width = this.ClientSize.Width - 150;
                currentContentPanel.Height = this.ClientSize.Height;
                sideMenuPanel.Height = this.ClientSize.Height - 170;
            };

            // Показать начальный экран
            ShowRatesContent();
        }

        private void ShowTransactionHistory()
        {
            // Заголовок
            Label titleLabel = new Label
            {
                Text = "История операций",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, 30),
                AutoSize = true
            };
            currentContentPanel.Controls.Add(titleLabel);

            // Поле поиска
            TextBox searchTextBox = new TextBox
            {
                Size = new Size(200, 30),
                Location = new Point(30, 70),
                BackColor = Color.FromArgb(40, 40, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Поиск..."
            };
            currentContentPanel.Controls.Add(searchTextBox);

            // Кнопка поиска
            Button searchButton = new Button
            {
                Text = "Поиск",
                Size = new Size(80, 30),
                Location = new Point(240, 70),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            searchButton.FlatAppearance.BorderSize = 0;
            currentContentPanel.Controls.Add(searchButton);

            // Кнопка удаления
            Button deleteButton = new Button
            {
                Text = "Удалить",
                Size = new Size(80, 30),
                Location = new Point(330, 70),
                BackColor = Color.FromArgb(180, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            deleteButton.FlatAppearance.BorderSize = 0;
            currentContentPanel.Controls.Add(deleteButton);

            // DataGridView для отображения истории транзакций
            DataGridView historyGridView = new DataGridView
            {
                Location = new Point(30, 110),
                Size = new Size(currentContentPanel.Width - 60, currentContentPanel.Height - 140),
                BackgroundColor = Color.FromArgb(25, 25, 30),
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // Настройка стиля DataGridView
            historyGridView.DefaultCellStyle.BackColor = Color.FromArgb(40, 40, 45);
            historyGridView.DefaultCellStyle.ForeColor = Color.White;
            historyGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 130, 180);
            historyGridView.DefaultCellStyle.SelectionForeColor = Color.White;
            historyGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(60, 60, 70);
            historyGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            historyGridView.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            historyGridView.EnableHeadersVisualStyles = false;

            // Загрузка данных из базы данных
            LoadTransactionHistory(historyGridView);

            // Функция для фильтрации данных
            // Функция для фильтрации данных
            void FilterData(string searchText)
            {
                if (historyGridView.DataSource is DataTable dataTable)
                {
                    if (string.IsNullOrWhiteSpace(searchText))
                    {
                        dataTable.DefaultView.RowFilter = "";
                    }
                    else
                    {
                        // Формируем выражение фильтра только для строковых полей
                        string filterExpression = $"CONVERT([Дата], 'System.String') LIKE '%{searchText}%' OR " +
                                               $"[Работник] LIKE '%{searchText}%' OR " +
                                               $"[Обмениваемая] LIKE '%{searchText}%' OR " +
                                               $"[Полученная] LIKE '%{searchText}%'";

                        // Если поисковый текст - число, добавляем фильтрацию по числовым полям
                        if (decimal.TryParse(searchText, out decimal numericValue))
                        {
                            filterExpression += $" OR [Сумма] = {numericValue} OR [Сумма_полученная] = {numericValue}";
                        }

                        try
                        {
                            dataTable.DefaultView.RowFilter = filterExpression;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка фильтрации: {ex.Message}", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            // Обработчики событий для поиска
            searchButton.Click += (sender, e) => FilterData(searchTextBox.Text);
            searchTextBox.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    FilterData(searchTextBox.Text);
                }
            };

            // Обработчик для кнопки удаления
            deleteButton.Click += (sender, e) =>
            {
                if (historyGridView.SelectedRows.Count > 0)
                {
                    int selectedRowIndex = historyGridView.SelectedRows[0].Index;
                    DataGridViewRow selectedRow = historyGridView.Rows[selectedRowIndex];
                    int transactionId = Convert.ToInt32(selectedRow.Cells["ID"].Value);

                    DialogResult result = MessageBox.Show(
                        "Вы уверены, что хотите удалить выбранную транзакцию?",
                        "Подтверждение удаления",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        DeleteTransaction(transactionId);
                        LoadTransactionHistory(historyGridView); // Обновляем таблицу после удаления
                    }
                }
                else
                {
                    MessageBox.Show("Выберите транзакцию для удаления", "Информация",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            currentContentPanel.Controls.Add(historyGridView);
        }

        private void ShowRatesContent()
        {
            // Заголовок
            Label titleLabel = new Label
            {
                Text = "Текущий курс рубля",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, 30),
                AutoSize = true
            };
            currentContentPanel.Controls.Add(titleLabel);

            // Первый столбец курсов
            int yPos = 70;

            string[] firstColumnCurrencies = { "USD", "EUR", "CNY", "UAH", "JPY" };

            foreach (var currency in firstColumnCurrencies)
            {
                Label currencyLabel = new Label
                {
                    Text = currency + ":",
                    ForeColor = Color.White,
                    Location = new Point(50, yPos),
                    Font = new Font("Segoe UI", 14),
                    AutoSize = true
                };
                currentContentPanel.Controls.Add(currencyLabel);

                Label rateLabel = new Label
                {
                    Text = Give_Rate_for_Ruble(currency),
                    ForeColor = Color.LightGreen,
                    Location = new Point(150, yPos),
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    AutoSize = true
                };
                currentContentPanel.Controls.Add(rateLabel);

                yPos += 40;
            }

            // Второй столбец курсов
            yPos = 70;
            string[] secondColumnCurrencies = { "BYN", "KRW", "AED", "PLN", "KZT" };

            foreach (var currency in secondColumnCurrencies)
            {
                Label currencyLabel = new Label
                {
                    Text = currency + ":",
                    ForeColor = Color.White,
                    Location = new Point(300, yPos),
                    Font = new Font("Segoe UI", 14),
                    AutoSize = true
                };
                currentContentPanel.Controls.Add(currencyLabel);

                Label rateLabel = new Label
                {
                    Text = Give_Rate_for_Ruble(currency),
                    ForeColor = Color.LightGreen,
                    Location = new Point(400, yPos),
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    AutoSize = true
                };
                currentContentPanel.Controls.Add(rateLabel);

                yPos += 40;
            }
            DateTime date1 = DateTime.Now;
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand("SELECT c.update_date FROM obmennik.currencies c WHERE c.id = 1", connection))
                {
                    date1 = Convert.ToDateTime(command.ExecuteScalar());
                }
            }
            // Время обновления
            Label updateLabel = new Label
            {
                Text = $"Обновлено: {date1:dd.MM.yy}",
                ForeColor = Color.LightGray,
                Location = new Point(50, 270),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
            currentContentPanel.Controls.Add(updateLabel);
        }

        private void ShowExchangeContent()
        {
            // Радиокнопки для выбора типа операции
            Label operationTypeLabel = new Label
            {
                Text = "Тип операции:",
                ForeColor = Color.White,
                Location = new Point(50, 30),
                Font = new Font("Segoe UI", 12),
                AutoSize = true
            };
            currentContentPanel.Controls.Add(operationTypeLabel);

            RadioButton rubToCurrency = new RadioButton
            {
                Text = "Рубли → Валюта",
                ForeColor = Color.White,
                Location = new Point(50, 60),
                Font = new Font("Segoe UI", 11),
                AutoSize = true,
                Checked = true
            };
            currentContentPanel.Controls.Add(rubToCurrency);

            RadioButton currencyToRub = new RadioButton
            {
                Text = "Валюта → Рубли",
                ForeColor = Color.White,
                Location = new Point(250, 60),
                Font = new Font("Segoe UI", 11),
                AutoSize = true
            };
            currentContentPanel.Controls.Add(currencyToRub);

            // Сумма для обмена
            Label amountLabel = new Label
            {
                Text = "Сумма:",
                ForeColor = Color.White,
                Location = new Point(50, 100),
                Font = new Font("Segoe UI", 12),
                AutoSize = true
            };
            currentContentPanel.Controls.Add(amountLabel);

            TextBox amountTextBox = new TextBox
            {
                Size = new Size(200, 35),
                Location = new Point(50, 130),
                BackColor = Color.FromArgb(40, 40, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11)
            };
            currentContentPanel.Controls.Add(amountTextBox);

            // Выбор валюты
            Label currencyLabel = new Label
            {
                Text = "Валюта:",
                ForeColor = Color.White,
                Location = new Point(270, 100),
                Font = new Font("Segoe UI", 12),
                AutoSize = true
            };
            currentContentPanel.Controls.Add(currencyLabel);

            ComboBox currencyComboBox = new ComboBox
            {
                Size = new Size(150, 35),
                Location = new Point(270, 130),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(40, 40, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11)
            };
            currencyComboBox.Items.AddRange(new string[] { "USD", "EUR", "CNY", "UAH", "JPY", "BYN", "KRW", "AED", "PLN", "KZT" });
            currencyComboBox.SelectedIndex = 0;
            currentContentPanel.Controls.Add(currencyComboBox);

            // Кнопка расчета
            Button calculateButton = new Button
            {
                Text = "Рассчитать",
                Size = new Size(currentContentPanel.Width - 100, 40),
                Location = new Point(50, 180),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12)
            };
            calculateButton.FlatAppearance.BorderSize = 0;
            currentContentPanel.Controls.Add(calculateButton);

            // Результат обмена
            Label resultTitleLabel = new Label
            {
                Text = "Вы получите:",
                ForeColor = Color.White,
                Location = new Point(50, 240),
                Font = new Font("Segoe UI", 14),
                AutoSize = true
            };
            currentContentPanel.Controls.Add(resultTitleLabel);

            Label resultValueLabel = new Label
            {
                Text = "0.00",
                ForeColor = Color.LightGreen,
                Location = new Point(180, 240),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true
            };
            currentContentPanel.Controls.Add(resultValueLabel);

            Label resultCurrencyLabel = new Label
            {
                Text = rubToCurrency.Checked ? currencyComboBox.SelectedItem.ToString() : "RUB",
                ForeColor = Color.LightGreen,
                Location = new Point(260, 240),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true
            };
            currentContentPanel.Controls.Add(resultCurrencyLabel);

            // Доступный баланс
            Label balanceTitleLabel = new Label
            {
                Text = "Доступно:",
                ForeColor = Color.White,
                Location = new Point(50, 280),
                Font = new Font("Segoe UI", 12),
                AutoSize = true
            };
            currentContentPanel.Controls.Add(balanceTitleLabel);
            // Кол-во руб на балике

            Label balanceValueLabel = new Label
            {
                Text = Give_Balance("RUB").ToString("N2") + " RUB",
                ForeColor = Color.LightGreen,
                Location = new Point(150, 280),
                Font = new Font("Segoe UI", 12),
                AutoSize = true
            };
            currentContentPanel.Controls.Add(balanceValueLabel);

            // Кнопка обмена
            Button exchangeButton = new Button
            {
                Text = "Обменять",
                Size = new Size(currentContentPanel.Width - 100, 50),
                Location = new Point(50, 320),
                BackColor = Color.FromArgb(80, 160, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            exchangeButton.FlatAppearance.BorderSize = 0;
            currentContentPanel.Controls.Add(exchangeButton);

            // Обработчики событий
            rubToCurrency.CheckedChanged += (sender, e) =>
            {
                if (rubToCurrency.Checked)
                {
                    resultCurrencyLabel.Text = currencyComboBox.SelectedItem.ToString();
                    balanceValueLabel.Text = Give_Balance("RUB").ToString("N2") + " RUB";
                }
            };

            currencyToRub.CheckedChanged += (sender, e) =>
            {
                if (currencyToRub.Checked)
                {
                    resultCurrencyLabel.Text = "RUB";
                    balanceValueLabel.Text = Give_Balance(currencyComboBox.SelectedItem.ToString()).ToString("N2") +
                                           " " + currencyComboBox.SelectedItem.ToString();
                }
            };

            currencyComboBox.SelectedIndexChanged += (sender, e) =>
            {
                if (rubToCurrency.Checked)
                {
                    resultCurrencyLabel.Text = currencyComboBox.SelectedItem.ToString();
                }
                else
                {
                    balanceValueLabel.Text = Give_Balance(currencyComboBox.SelectedItem.ToString()).ToString("N2") +
                                           " " + currencyComboBox.SelectedItem.ToString();
                }
            };

            calculateButton.Click += (sender, e) =>
            {
                if (decimal.TryParse(amountTextBox.Text, out decimal amount))
                {
                    string currency = currencyComboBox.SelectedItem.ToString();

                    if (amount <= 0)
                    {
                        resultValueLabel.Text = "Ошибка";
                        return;
                    }

                    decimal result;
                    if (rubToCurrency.Checked)
                    {
                        decimal effectiveRate = Give_Rate(currency) * 0.90m;
                        result = amount / effectiveRate;
                        resultValueLabel.Text = result.ToString("N2");
                    }
                    else
                    {
                        decimal effectiveRate = Give_Rate(currency) * 1.10m;
                        result = amount * effectiveRate;
                        resultValueLabel.Text = result.ToString("N2");
                    }
                }
                else
                {
                    resultValueLabel.Text = "Ошибка";
                }
            };

            exchangeButton.Click += (sender, e) =>
            {
                if (decimal.TryParse(amountTextBox.Text, out decimal amount))
                {
                    string currency = currencyComboBox.SelectedItem.ToString();

                    if (amount <= 0)
                    {
                        MessageBox.Show("Сумма должна быть больше нуля", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    decimal result;
                    if (rubToCurrency.Checked)
                    {
                        if (amount > Give_Balance("RUB"))
                        {
                            MessageBox.Show("Недостаточно рублей", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        decimal effectiveRate = Give_Rate(currency) * 0.90m;
                        result = amount / effectiveRate;

                        decimal giving_balance = Give_Balance("RUB");
                        giving_balance -= amount;
                        Set_Balance("RUB", giving_balance);

                        decimal receiving_balance = Give_Balance(currency);
                        receiving_balance += result;
                        Set_Balance(currency, receiving_balance);
                        balanceValueLabel.Text = Give_Balance("RUB").ToString("N2") + " RUB";
                        float floatValue = (float)effectiveRate;
                        SaveTransactionToDatabase(
                        "RUB",
                        currency,
                        amount,
                        result,
                        floatValue,
                        employee);
                    }
                    else
                    {
                        if (amount > Give_Balance(currency))
                        {
                            MessageBox.Show($"Недостаточно {currency}", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            return;
                        }

                        decimal effectiveRate = Give_Rate(currency) * 1.10m;
                        result = amount * effectiveRate;

                        decimal receiving_balance = Give_Balance(currency);

                        receiving_balance -= amount;
                        Set_Balance(currency, receiving_balance);

                        decimal giving_balance = Give_Balance("RUB");
                        giving_balance += result;
                        Set_Balance("RUB", giving_balance);
                        balanceValueLabel.Text = Give_Balance(currency).ToString("N2") + " " + currency;

                        float floatValue = (float)effectiveRate;
                        SaveTransactionToDatabase(
                        currency,
                        "RUB",
                        amount,
                        result,
                        floatValue,
                        employee);
                    }

                    resultValueLabel.Text = result.ToString("N2");

                    // Сохранение транзакции в базу данных


                    MessageBox.Show($"Обмен выполнен!\nПолучено: {result:N2} {resultCurrencyLabel.Text}",
                        "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Введите корректную сумму", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
        }
    }
}