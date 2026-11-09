using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace KlassKlass
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            LoadUsers();
            LoadSubjects();
        }

        private void LoadUsers()
        {
            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery(
                    "SELECT id, login, password, fio, role, ISNULL(group_name, '-') as group_name FROM users ORDER BY id");
                DgUsers.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пользователей: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSubjects()
        {
            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery(
                    "SELECT id, name, group_name, color FROM subjects ORDER BY id");
                DgSubjects.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки предметов: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int userId = Convert.ToInt32(btn.Tag);

                var result = MessageBox.Show(
                    "Удалить пользователя и все его работы?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        DatabaseHelper.ExecuteNonQuery(
                            "DELETE FROM assignment_files WHERE student_id = @id",
                            new SqlParameter("@id", userId));

                        DatabaseHelper.ExecuteNonQuery(
                            "DELETE FROM assignment_files WHERE assignment_id IN (SELECT id FROM assignments WHERE subject_id IN (SELECT id FROM subjects WHERE teacher_id = @id))",
                            new SqlParameter("@id", userId));

                        DatabaseHelper.ExecuteNonQuery(
                            "DELETE FROM assignments WHERE subject_id IN (SELECT id FROM subjects WHERE teacher_id = @id)",
                            new SqlParameter("@id", userId));

                        DatabaseHelper.ExecuteNonQuery(
                            "DELETE FROM subjects WHERE teacher_id = @id",
                            new SqlParameter("@id", userId));

                        DatabaseHelper.ExecuteNonQuery(
                            "DELETE FROM users WHERE id = @id",
                            new SqlParameter("@id", userId));

                        MessageBox.Show("Пользователь удален!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadUsers();
                        LoadSubjects();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnDeleteSubject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int subjectId = Convert.ToInt32(btn.Tag);

                var result = MessageBox.Show(
                    "Удалить предмет и все задания по нему?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        DatabaseHelper.ExecuteNonQuery(
                            "DELETE FROM assignment_files WHERE assignment_id IN (SELECT id FROM assignments WHERE subject_id = @id)",
                            new SqlParameter("@id", subjectId));

                        DatabaseHelper.ExecuteNonQuery(
                            "DELETE FROM assignments WHERE subject_id = @id",
                            new SqlParameter("@id", subjectId));

                        DatabaseHelper.ExecuteNonQuery(
                            "DELETE FROM subjects WHERE id = @id",
                            new SqlParameter("@id", subjectId));

                        MessageBox.Show("Предмет удален!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadSubjects();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            new WelcomeWindow().Show();
            Close();
        }
    }
}