using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace KlassKlass
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
            LblGroup.Visibility = Visibility.Visible;
            BrGroup.Visibility = Visibility.Visible;
        }

        private void CmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbRole.SelectedItem is ComboBoxItem item)
            {
                bool isStudent = item.Content.ToString() == "Ученик";
                LblGroup.Visibility = isStudent ? Visibility.Visible : Visibility.Collapsed;
                BrGroup.Visibility = isStudent ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtRegLogin.Text.Trim();
            string fio = TxtRegFIO.Text.Trim();
            string password = TxtRegPassword.Password;
            string role = (CmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();
            string group = (CmbGroup.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(fio) ||
                string.IsNullOrEmpty(password) || role == null)
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string dbRole = role == "Ученик" ? "student" : "teacher";
            object groupValue = role == "Ученик" ? (object)group : DBNull.Value;

            try
            {
                DatabaseHelper.ExecuteNonQuery(
                    "INSERT INTO users (login, password, fio, role, group_name) VALUES (@login,@password,@fio,@role,@group)",
                    new SqlParameter("@login", login),
                    new SqlParameter("@password", password),
                    new SqlParameter("@fio", fio),
                    new SqlParameter("@role", dbRole),
                    new SqlParameter("@group", groupValue));

                MessageBox.Show("Регистрация успешна! Теперь войдите.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}