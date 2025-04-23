using System;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        String ConnectionString = "Host = 91.233.173.91; Username = selectel; Password = selectel; Database = selectel";
        public Form1()
        {
            InitializeComponent();
            InitializeAuthForm();
        }

        private void InitializeAuthForm()
        {
            // Настройки формы (компактный размер)
            this.Text = "Bushido Bucks - Авторизация";
            this.ClientSize = new Size(350, 400); // Уменьшенная форма
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(25, 25, 30);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Логотип (центрированный)
            PictureBox logoPictureBox = new PictureBox
            {
                Size = new Size(250, 120),
                Location = new Point((this.ClientSize.Width - 250) / 2, 20),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = Properties.Resources.logo
            };
            this.Controls.Add(logoPictureBox);

            // Метка для логина
            Label loginLabel = new Label
            {
                Text = "Логин:",
                ForeColor = Color.White,
                Location = new Point(25, 160),
                Font = new Font("Segoe UI", 10),
                AutoSize = true
            };
            this.Controls.Add(loginLabel);

            // Поле для логина
            TextBox loginTextBox = new TextBox
            {
                Size = new Size(300, 30),
                Location = new Point(25, 185),
                BackColor = Color.FromArgb(40, 40, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(loginTextBox);

            // Метка для пароля
            Label passwordLabel = new Label
            {
                Text = "Пароль:",
                ForeColor = Color.White,
                Location = new Point(25, 230),
                Font = new Font("Segoe UI", 10),
                AutoSize = true
            };
            this.Controls.Add(passwordLabel);

            // Поле для пароля
            TextBox passwordTextBox = new TextBox
            {
                Size = new Size(300, 30),
                Location = new Point(25, 255),
                BackColor = Color.FromArgb(40, 40, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                PasswordChar = '•'
            };
            this.Controls.Add(passwordTextBox);

            // Кнопка входа (центрированная)
            Button loginButton = new Button
            {
                Text = "Войти",
                Size = new Size(300, 40), // Шире для лучшего визуального восприятия
                Location = new Point(25, 310), // Центрирование по горизонтали
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            loginButton.FlatAppearance.BorderSize = 0;
            loginButton.Click += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(loginTextBox.Text) ||
                    string.IsNullOrWhiteSpace(passwordTextBox.Text))
                {
                    MessageBox.Show("Введите логин и пароль", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(ConnectionString))
                    {
                        // Проверка существования логина
                        NpgsqlCommand command = new NpgsqlCommand("SELECT COUNT(*) FROM obmennik.employees WHERE login = @a", npgsqlConnection);
                        npgsqlConnection.Open();
                        string login = loginTextBox.Text;
                        command.Parameters.AddWithValue("@a", login);
                        int d = Convert.ToInt32(command.ExecuteScalar());
                        npgsqlConnection.Close();

                        if (d > 0)
                        {
                            // Проверка логина и пароля
                            NpgsqlCommand command1 = new NpgsqlCommand("SELECT COUNT(*) FROM obmennik.employees WHERE login = @l AND password = @p", npgsqlConnection);
                            command1.Parameters.AddWithValue("@l", loginTextBox.Text);
                            command1.Parameters.AddWithValue("@p", passwordTextBox.Text);
                            npgsqlConnection.Open();
                            int a = Convert.ToInt32(command1.ExecuteScalar());
                            npgsqlConnection.Close();
                            string fio =  "";
                            int emloyee = 0;
                            NpgsqlCommand command2 = new NpgsqlCommand("SELECT fio, id FROM obmennik.employees WHERE login = @l AND password = @p", npgsqlConnection);
                            npgsqlConnection.Open();
                            command2.Parameters.AddWithValue("@l", loginTextBox.Text);
                            command2.Parameters.AddWithValue("@p", passwordTextBox.Text); 
                            NpgsqlDataReader reader1 = command2.ExecuteReader();
                            if (reader1.Read())
                            {
                                fio = reader1.GetString(0);
                                emloyee = reader1.GetInt32(1);
                            }
                            npgsqlConnection.Close();

                            if (a > 0)
                            {
                                MessageBox.Show($"Добро пожаловать, {fio}!",
                                "Успешная авторизация",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                                Main_menu signin = new Main_menu();
                                signin.employee = emloyee;
                                signin.FormClosed += (s, args) => this.Close();
                                signin.Show();
                                this.Hide();
                            }
                            else
                            {
                                MessageBox.Show("Неверный пароль", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Неверный логин", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            };
            this.Controls.Add(loginButton);
        }
    }
}