using Microsoft.Win32;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KlassKlass
{
    public partial class StudentWindow : Window
    {
        private int currentSubjectId = 0;
        private int currentAssignmentId = 0;

        public StudentWindow()
        {
            InitializeComponent();
            TxtUserFIO.Text = Session.FIO;
            TxtUserGroup.Text = Session.GroupName;
            LoadSubjects();
        }

        #region ЗАГРУЗКА ПРЕДМЕТОВ

        private void LoadSubjects()
        {
            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery(
                    @"SELECT s.id, s.name, s.color, u.fio as teacher_fio,
                             (SELECT TOP 1 a.title FROM assignments a WHERE a.subject_id = s.id ORDER BY a.created_at DESC) as last_assignment
                      FROM subjects s
                      JOIN users u ON s.teacher_id = u.id
                      WHERE s.group_name = @group",
                    new SqlParameter("@group", Session.GroupName));

                // Заполняем карточки в основной области
                SubjectsPanel.Children.Clear();

                foreach (DataRow row in dt.Rows)
                {
                    string color = row["color"].ToString();
                    string lastAssignment = row["last_assignment"] == DBNull.Value ? "Нет" : row["last_assignment"].ToString();

                    var card = new Border
                    {
                        Width = 280,
                        Height = 160,
                        Margin = new Thickness(10),
                        CornerRadius = new CornerRadius(12),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                        BorderThickness = new Thickness(1),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    // Цветная шапка
                    var headerBorder = new Border
                    {
                        Height = 70,
                        CornerRadius = new CornerRadius(12, 12, 0, 0),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                        Padding = new Thickness(15, 10, 15, 10)
                    };

                    var headerStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    headerStack.Children.Add(new TextBlock
                    {
                        Text = row["name"].ToString(),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        TextWrapping = TextWrapping.Wrap
                    });
                    headerStack.Children.Add(new TextBlock
                    {
                        Text = row["teacher_fio"].ToString(),
                        FontSize = 12,
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 3, 0, 0)
                    });
                    headerBorder.Child = headerStack;

                    // Белая часть с заданием
                    var bodyBorder = new Border
                    {
                        Height = 90,
                        CornerRadius = new CornerRadius(0, 0, 12, 12),
                        Background = Brushes.White,
                        Padding = new Thickness(15, 10, 15, 10)
                    };

                    var bodyStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    bodyStack.Children.Add(new TextBlock
                    {
                        Text = "Задание:",
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333"))
                    });
                    bodyStack.Children.Add(new TextBlock
                    {
                        Text = lastAssignment,
                        FontSize = 13,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                        Margin = new Thickness(0, 3, 0, 0)
                    });
                    bodyBorder.Child = bodyStack;

                    var mainStack = new StackPanel();
                    mainStack.Children.Add(headerBorder);
                    mainStack.Children.Add(bodyBorder);
                    card.Child = mainStack;

                    int subjectId = Convert.ToInt32(row["id"]);
                    card.MouseLeftButtonDown += (s, ev) => OpenSubject(subjectId);

                    SubjectsPanel.Children.Add(card);

                    // Заполняем сайдбар
                    var sidebarItem = new Border
                    {
                        Background = Brushes.Transparent,
                        Padding = new Thickness(10, 8, 10, 8),
                        Margin = new Thickness(0, 2, 0, 2),
                        CornerRadius = new CornerRadius(6),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    sidebarItem.Child = new TextBlock
                    {
                        Text = row["name"].ToString(),
                        FontSize = 12,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555")),
                        TextWrapping = TextWrapping.Wrap
                    };
                    sidebarItem.MouseEnter += (s, ev) => sidebarItem.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
                    sidebarItem.MouseLeave += (s, ev) => sidebarItem.Background = Brushes.Transparent;
                    sidebarItem.MouseLeftButtonDown += (s, ev) => OpenSubject(subjectId);

                    SidebarSubjectsList.Children.Add(sidebarItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки предметов: " + ex.Message);
            }
        }

        #endregion

        #region ЭКРАН ПРЕДМЕТА (Фото 2)

        private void OpenSubject(int subjectId)
        {
            currentSubjectId = subjectId;

            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery(
                    @"SELECT s.name, s.group_name, s.color
                      FROM subjects s WHERE s.id = @id",
                    new SqlParameter("@id", subjectId));

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    TxtSubjectTitle.Text = row["name"].ToString();
                    TxtSubjectGroup.Text = row["group_name"].ToString();
                }

                // Загружаем задания
                DataTable assignDt = DatabaseHelper.ExecuteQuery(
                    @"SELECT id, title, description, created_at
                      FROM assignments WHERE subject_id = @sid
                      ORDER BY created_at ASC",
                    new SqlParameter("@sid", subjectId));

                AssignmentsList.Children.Clear();

                if (assignDt.Rows.Count == 0)
                {
                    TxtEmptyAssignments.Visibility = Visibility.Visible;
                }
                else
                {
                    TxtEmptyAssignments.Visibility = Visibility.Collapsed;

                    foreach (DataRow row in assignDt.Rows)
                    {
                        var btn = new Border
                        {
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                            CornerRadius = new CornerRadius(20),
                            Padding = new Thickness(20, 12, 20, 12),
                            Margin = new Thickness(0, 0, 0, 10),
                            Cursor = System.Windows.Input.Cursors.Hand,
                            Width = 400,
                            HorizontalAlignment = HorizontalAlignment.Left
                        };

                        btn.Child = new TextBlock
                        {
                            Text = row["title"].ToString(),
                            FontSize = 15,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333"))
                        };

                        int assignId = Convert.ToInt32(row["id"]);
                        btn.MouseLeftButtonDown += (s, ev) => OpenAssignment(assignId);

                        AssignmentsList.Children.Add(btn);
                    }
                }

                MainPanel.Visibility = Visibility.Collapsed;
                SubjectPanel.Visibility = Visibility.Visible;
                AssignmentPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void BtnBackToMain_Click(object sender, RoutedEventArgs e)
        {
            MainPanel.Visibility = Visibility.Visible;
            SubjectPanel.Visibility = Visibility.Collapsed;
            AssignmentPanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region ЭКРАН ЗАДАНИЯ (Фото 3)

        private void OpenAssignment(int assignmentId)
        {
            currentAssignmentId = assignmentId;

            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery(
                    @"SELECT a.title, a.description, 
                             ISNULL((SELECT TOP 1 af.grade FROM assignment_files af 
                                     WHERE af.assignment_id = a.id AND af.student_id = @uid 
                                     ORDER BY af.submitted_at DESC), 0) as grade
                      FROM assignments a WHERE a.id = @id",
                    new SqlParameter("@id", assignmentId),
                    new SqlParameter("@uid", Session.UserId));

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    TxtAssignmentTitle.Text = row["title"].ToString();
                    TxtGrade.Text = row["grade"].ToString();
                    TxtAssignmentDesc.Text = row["description"] == DBNull.Value ? "" : row["description"].ToString();
                }

                LoadStudentFiles();

                MainPanel.Visibility = Visibility.Collapsed;
                SubjectPanel.Visibility = Visibility.Collapsed;
                AssignmentPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void BtnBackToSubject_Click(object sender, RoutedEventArgs e)
        {
            MainPanel.Visibility = Visibility.Collapsed;
            SubjectPanel.Visibility = Visibility.Visible;
            AssignmentPanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region ФАЙЛЫ РАБОТЫ

        private void LoadStudentFiles()
        {
            FilesPanel.Children.Clear();

            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery(
                    @"SELECT id, file_name, file_path FROM assignment_files 
                      WHERE assignment_id = @aid AND student_id = @uid
                      ORDER BY submitted_at ASC",
                    new SqlParameter("@aid", currentAssignmentId),
                    new SqlParameter("@uid", Session.UserId));

                int fileCount = dt.Rows.Count;

                foreach (DataRow row in dt.Rows)
                {
                    int fileId = Convert.ToInt32(row["id"]);
                    string fileName = row["file_name"].ToString();
                    string ext = Path.GetExtension(fileName).ToLower();

                    var fileCard = new Border
                    {
                        Width = 80,
                        Height = 80,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                        CornerRadius = new CornerRadius(10),
                        Margin = new Thickness(5),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    // Иконка файла
                    string icon = "📄";
                    if (ext == ".pdf") icon = "📕";
                    else if (ext == ".docx" || ext == ".doc") icon = "📘";
                    else if (ext == ".jpg" || ext == ".png") icon = "🖼️";
                    else if (ext == ".xlsx" || ext == ".xls") icon = "📗";

                    var stack = new StackPanel
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    stack.Children.Add(new TextBlock
                    {
                        Text = icon,
                        FontSize = 28,
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                    stack.Children.Add(new TextBlock
                    {
                        Text = Path.GetFileNameWithoutExtension(fileName).Length > 8
                            ? Path.GetFileNameWithoutExtension(fileName).Substring(0, 8) + "..."
                            : Path.GetFileNameWithoutExtension(fileName),
                        FontSize = 9,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });
                    fileCard.Child = stack;

                    // Кнопка удаления (мусорка в правом верхнем углу)
                    var deleteBtn = new Button
                    {
                        Content = "🗑",
                        Width = 22,
                        Height = 22,
                        FontSize = 11,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(-12, -10, 0, 0)
                    };
                    deleteBtn.Click += (s, ev) => DeleteFile(fileId, fileName);

                    // Контейнер для карточки + кнопки удаления
                    var container = new Grid();
                    container.Children.Add(fileCard);
                    container.Children.Add(deleteBtn);

                    FilesPanel.Children.Add(container);
                }

                // Показываем/скрываем кнопку добавления
                BtnAddFile.Visibility = fileCount >= 4 ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки файлов: " + ex.Message);
            }
        }

        private void BtnAddFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Выберите файл работы";
            ofd.Filter = "Все файлы|*.*|PDF|*.pdf|Word|*.docx;*.doc|Images|*.jpg;*.png|Excel|*.xlsx;*.xls";

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string fileName = Path.GetFileName(ofd.FileName);
                    string uploadDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");
                    Directory.CreateDirectory(uploadDir);
                    string destPath = Path.Combine(uploadDir, $"{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}");
                    File.Copy(ofd.FileName, destPath);

                    DatabaseHelper.ExecuteNonQuery(
                        "INSERT INTO assignment_files (assignment_id, student_id, file_path, file_name, grade) VALUES (@aid,@sid,@fp,@fn,0)",
                        new SqlParameter("@aid", currentAssignmentId),
                        new SqlParameter("@sid", Session.UserId),
                        new SqlParameter("@fp", destPath),
                        new SqlParameter("@fn", fileName));

                    LoadStudentFiles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка загрузки файла: " + ex.Message);
                }
            }
        }

        private void DeleteFile(int fileId, string fileName)
        {
            var result = MessageBox.Show(
                $"Вы хотите удалить файл \"{fileName}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Получаем путь к файлу
                    DataTable dt = DatabaseHelper.ExecuteQuery(
                        "SELECT file_path FROM assignment_files WHERE id = @id",
                        new SqlParameter("@id", fileId));

                    if (dt.Rows.Count > 0)
                    {
                        string filePath = dt.Rows[0]["file_path"].ToString();
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }

                    DatabaseHelper.ExecuteNonQuery(
                        "DELETE FROM assignment_files WHERE id = @id",
                        new SqlParameter("@id", fileId));

                    LoadStudentFiles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка удаления: " + ex.Message);
                }
            }
        }

        #endregion

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            new WelcomeWindow().Show();
            Close();
        }
    }
}