using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace KlassKlass
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnDoLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtLogin.Text.Trim();
            string password = TxtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery(
                    "SELECT * FROM users WHERE login=@login AND password=@password",
                    new SqlParameter("@login", login),
                    new SqlParameter("@password", password));

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    Session.UserId = Convert.ToInt32(row["id"]);
                    Session.Login = row["login"].ToString();
                    Session.FIO = row["fio"].ToString();
                    Session.Role = row["role"].ToString();
                    Session.GroupName = row["group_name"]?.ToString() ?? "";

                    if (Session.Role == "admin")
                        OpenWindow(new AdminWindow());
                    else if (Session.Role == "teacher")
                        OpenWindow(new TeacherWindow());
                    else
                        OpenWindow(new StudentWindow());
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtRegister_Click(object sender, MouseButtonEventArgs e)
        {
            var regWindow = new RegisterWindow();
            regWindow.Owner = this;
            regWindow.ShowDialog();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            var welcome = new WelcomeWindow();
            welcome.Show();
            this.Close();
        }

        private void OpenWindow(Window window)
        {
            window.Show();
            this.Close();
        }
    }
}