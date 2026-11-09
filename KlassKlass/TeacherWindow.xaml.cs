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
    public partial class TeacherWindow : Window
    {
        private int currentSubjectId = 0;
        private int currentAssignmentId = 0;
        private int currentStudentId = 0;
        private int currentFileId = 0;

        public TeacherWindow()
        {
            InitializeComponent();
            TxtUserFIO.Text = Session.FIO;
            LoadSubjects();
        }

        #region ЗАГРУЗКА ПРЕДМЕТОВ

        private void LoadSubjects()
        {
            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery(
                    @"SELECT s.id, s.name, s.color, s.group_name,
                             (SELECT TOP 1 a.title FROM assignments a WHERE a.subject_id = s.id ORDER BY a.created_at DESC) as last_assignment
                      FROM subjects s
                      WHERE s.teacher_id = @tid",
                    new SqlParameter("@tid", Session.UserId));

                SubjectsPanel.Children.Clear();
                SidebarSubjectsList.Children.Clear();

                foreach (DataRow row in dt.Rows)
                {
                    string color = row["color"].ToString();
                    string lastAssignment = row["last_assignment"] == DBNull.Value ? "Нет" : row["last_assignment"].ToString();
                    int subjectId = Convert.ToInt32(row["id"]);

                    // Карточка предмета
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
                        Text = row["group_name"].ToString(),
                        FontSize = 12,
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 3, 0, 0)
                    });
                    headerBorder.Child = headerStack;

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

                    card.MouseLeftButtonDown += (s, ev) => OpenSubject(subjectId);
                    SubjectsPanel.Children.Add(card);

                    // Сайдбар
                    var sidebarItem = new Border
                    {
                        Background = Brushes.Transparent,
                        Padding = new Thickness(10, 8, 10, 8),
                        Margin = new Thickness(0, 2, 0, 2),
                        CornerRadius = new CornerRadius(6),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    sidebarItem.Child = new StackPanel();
                    ((StackPanel)sidebarItem.Child).Children.Add(new TextBlock
                    {
                        Text = row["name"].ToString(),
                        FontSize = 12,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555")),
                        TextWrapping = TextWrapping.Wrap
                    });
                    ((StackPanel)sidebarItem.Child).Children.Add(new TextBlock
                    {
                        Text = row["group_name"].ToString(),
                        FontSize = 11,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999")),
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                    sidebarItem.MouseEnter += (s, ev) => sidebarItem.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
                    sidebarItem.MouseLeave += (s, ev) => sidebarItem.Background = Brushes.Transparent;
                    sidebarItem.MouseLeftButtonDown += (s, ev) => OpenSubject(subjectId);
                    SidebarSubjectsList.Children.Add(sidebarItem);
                }

                // Кнопка "Создать предмет"
                var addCard = new Border
                {
                    Width = 280,
                    Height = 160,
                    Margin = new Thickness(10),
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#808080")),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                var addStack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                addStack.Children.Add(new TextBlock { Text = "+", FontSize = 48, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center });
                addStack.Children.Add(new TextBlock { Text = "Создать предмет", FontSize = 14, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center });
                addCard.Child = addStack;
                addCard.MouseLeftButtonDown += (s, ev) => ShowCreateSubjectDialog();
                SubjectsPanel.Children.Add(addCard);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void ShowCreateSubjectDialog()
        {
            var dialog = new Window
            {
                Title = "Создать предмет",
                Width = 350,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"))
            };

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "Название предмета:", FontSize = 13, Margin = new Thickness(0, 0, 0, 5) });
            var txtName = new TextBox { Height = 32, Margin = new Thickness(0, 0, 0, 10) };
            stack.Children.Add(txtName);

            stack.Children.Add(new TextBlock { Text = "Группа:", FontSize = 13, Margin = new Thickness(0, 0, 0, 5) });
            var cmbGroup = new ComboBox { Height = 32, Margin = new Thickness(0, 0, 0, 15) };
            cmbGroup.Items.Add(new ComboBoxItem { Content = "22ИС" });
            cmbGroup.Items.Add(new ComboBoxItem { Content = "25ИС" });
            stack.Children.Add(cmbGroup);

            var btnCreate = new Button
            {
                Content = "Создать",
                Width = 120,
                Height = 35,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4169E1")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCreate.Click += (s, ev) =>
            {
                string name = txtName.Text.Trim();
                string group = (cmbGroup.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (string.IsNullOrEmpty(name) || group == null)
                {
                    MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string[] colors = { "#E74C3C", "#9B59B6", "#2ECC71", "#F39C12", "#3498DB", "#1ABC9C", "#E67E22", "#95A5A6" };
                Random rnd = new Random();
                string randomColor = colors[rnd.Next(colors.Length)];

                try
                {
                    DatabaseHelper.ExecuteNonQuery(
                        "INSERT INTO subjects (name, group_name, teacher_id, color) VALUES (@name,@group,@tid,@color)",
                        new SqlParameter("@name", name),
                        new SqlParameter("@group", group),
                        new SqlParameter("@tid", Session.UserId),
                        new SqlParameter("@color", randomColor));

                    dialog.Close();
                    LoadSubjects();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            };
            stack.Children.Add(btnCreate);

            dialog.Content = stack;
            dialog.ShowDialog();
        }

        #endregion

        #region ЭКРАН ПРЕДМЕТА

        private void OpenSubject(int subjectId)
        {
            currentSubjectId = subjectId;

            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery(
                    "SELECT name, group_name FROM subjects WHERE id = @id",
                    new SqlParameter("@id", subjectId));

                if (dt.Rows.Count > 0)
                {
                    TxtSubjectTitle.Text = dt.Rows[0]["name"].ToString();
                    TxtSubjectGroup.Text = dt.Rows[0]["group_name"].ToString();
                }

                DataTable assignDt = DatabaseHelper.ExecuteQuery(
                    "SELECT id, title FROM assignments WHERE subject_id = @sid ORDER BY created_at ASC",
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
                SubmissionsPanel.Visibility = Visibility.Collapsed;
                ModalOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void BtnDeleteSubject_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Удалить предмет и все задания?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    DatabaseHelper.ExecuteNonQuery(
                        "DELETE FROM assignment_files WHERE assignment_id IN (SELECT id FROM assignments WHERE subject_id=@sid)",
                        new SqlParameter("@sid", currentSubjectId));
                    DatabaseHelper.ExecuteNonQuery(
                        "DELETE FROM assignments WHERE subject_id=@sid",
                        new SqlParameter("@sid", currentSubjectId));
                    DatabaseHelper.ExecuteNonQuery(
                        "DELETE FROM subjects WHERE id=@id",
                        new SqlParameter("@id", currentSubjectId));

                    BtnBackToMain_Click(null, null);
                    LoadSubjects();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }

        private void BtnBackToMain_Click(object sender, RoutedEventArgs e)
        {
            MainPanel.Visibility = Visibility.Visible;
            SubjectPanel.Visibility = Visibility.Collapsed;
            AssignmentPanel.Visibility = Visibility.Collapsed;
            SubmissionsPanel.Visibility = Visibility.Collapsed;
            ModalOverlay.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region ДОБАВЛЕНИЕ ЗАДАНИЯ

        private void BtnAddAssignment_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Добавить задание",
                Width = 400,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"))
            };

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "Название задания:", FontSize = 13, Margin = new Thickness(0, 0, 0, 5) });
            var txtTitle = new TextBox { Height = 32, Margin = new Thickness(0, 0, 0, 10) };
            stack.Children.Add(txtTitle);

            stack.Children.Add(new TextBlock { Text = "Описание:", FontSize = 13, Margin = new Thickness(0, 0, 0, 5) });
            var txtDesc = new TextBox { Height = 100, Margin = new Thickness(0, 0, 0, 15), TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            stack.Children.Add(txtDesc);

            var btnSave = new Button
            {
                Content = "Сохранить",
                Width = 120,
                Height = 35,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnSave.Click += (s, ev) =>
            {
                string title = txtTitle.Text.Trim();
                string desc = txtDesc.Text.Trim();
                if (string.IsNullOrEmpty(title))
                {
                    MessageBox.Show("Введите название!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    DatabaseHelper.ExecuteNonQuery(
                        "INSERT INTO assignments (subject_id, title, description) VALUES (@sid,@title,@desc)",
                        new SqlParameter("@sid", currentSubjectId),
                        new SqlParameter("@title", title),
                        new SqlParameter("@desc", desc));

                    dialog.Close();
                    OpenSubject(currentSubjectId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            };
            stack.Children.Add(btnSave);

            dialog.Content = stack;
            dialog.ShowDialog();
        }

        #endregion

        #region ЭКРАН ЗАДАНИЯ

        private void OpenAssignment(int assignmentId)
        {
            currentAssignmentId = assignmentId;

            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery(
                    "SELECT title, description FROM assignments WHERE id = @id",
                    new SqlParameter("@id", assignmentId));

                if (dt.Rows.Count > 0)
                {
                    TxtAssignmentTitle.Text = dt.Rows[0]["title"].ToString();
                    TxtAssignmentDesc.Text = dt.Rows[0]["description"] == DBNull.Value ? "" : dt.Rows[0]["description"].ToString();
                }

                MainPanel.Visibility = Visibility.Collapsed;
                SubjectPanel.Visibility = Visibility.Collapsed;
                AssignmentPanel.Visibility = Visibility.Visible;
                SubmissionsPanel.Visibility = Visibility.Collapsed;
                ModalOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void BtnEditAssignment_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Редактировать задание",
                Width = 400,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"))
            };

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "Название:", FontSize = 13, Margin = new Thickness(0, 0, 0, 5) });
            var txtTitle = new TextBox { Height = 32, Text = TxtAssignmentTitle.Text, Margin = new Thickness(0, 0, 0, 10) };
            stack.Children.Add(txtTitle);

            stack.Children.Add(new TextBlock { Text = "Описание:", FontSize = 13, Margin = new Thickness(0, 0, 0, 5) });
            var txtDesc = new TextBox { Height = 100, Text = TxtAssignmentDesc.Text, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 0, 0, 15) };
            stack.Children.Add(txtDesc);

            var btnSave = new Button
            {
                Content = "Сохранить",
                Width = 120,
                Height = 35,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4169E1")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnSave.Click += (s, ev) =>
            {
                try
                {
                    DatabaseHelper.ExecuteNonQuery(
                        "UPDATE assignments SET title=@title, description=@desc WHERE id=@id",
                        new SqlParameter("@title", txtTitle.Text.Trim()),
                        new SqlParameter("@desc", txtDesc.Text.Trim()),
                        new SqlParameter("@id", currentAssignmentId));

                    dialog.Close();
                    OpenAssignment(currentAssignmentId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            };
            stack.Children.Add(btnSave);

            dialog.Content = stack;
            dialog.ShowDialog();
        }

        private void BtnBackToSubject_Click(object sender, RoutedEventArgs e)
        {
            MainPanel.Visibility = Visibility.Collapsed;
            SubjectPanel.Visibility = Visibility.Visible;
            AssignmentPanel.Visibility = Visibility.Collapsed;
            SubmissionsPanel.Visibility = Visibility.Collapsed;
            ModalOverlay.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region СПИСОК УЧЕНИКОВ (СДАЛИ)

        private void BtnShowSubmissions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем группу предмета
                DataTable subjDt = DatabaseHelper.ExecuteQuery(
                    "SELECT group_name FROM subjects WHERE id = @id",
                    new SqlParameter("@id", currentSubjectId));
                string groupName = subjDt.Rows[0]["group_name"].ToString();

                // Получаем всех учеников группы
                DataTable studentsDt = DatabaseHelper.ExecuteQuery(
                    "SELECT id, fio FROM users WHERE role='student' AND group_name=@group",
                    new SqlParameter("@group", groupName));

                StudentsList.Children.Clear();

                foreach (DataRow row in studentsDt.Rows)
                {
                    int studentId = Convert.ToInt32(row["id"]);
                    string fio = row["fio"].ToString();

                    // Проверяем, сдал ли работу
                    DataTable checkDt = DatabaseHelper.ExecuteQuery(
                        "SELECT COUNT(*) as cnt FROM assignment_files WHERE assignment_id=@aid AND student_id=@sid",
                        new SqlParameter("@aid", currentAssignmentId),
                        new SqlParameter("@sid", studentId));

                    bool submitted = Convert.ToInt32(checkDt.Rows[0]["cnt"]) > 0;

                    var pill = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                        CornerRadius = new CornerRadius(20),
                        Padding = new Thickness(20, 12, 20, 12),
                        Margin = new Thickness(0, 0, 0, 10),
                        Width = 500,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var txtFio = new TextBlock
                    {
                        Text = fio,
                        FontSize = 15,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333")),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(txtFio, 0);

                    var txtStatus = new TextBlock
                    {
                        Text = submitted ? "Сдал" : "Не сдал",
                        FontSize = 14,
                        Foreground = submitted ? Brushes.Green : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666")),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(15, 0, 0, 0)
                    };
                    Grid.SetColumn(txtStatus, 1);

                    grid.Children.Add(txtFio);
                    grid.Children.Add(txtStatus);
                    pill.Child = grid;

                    if (submitted)
                    {
                        pill.Cursor = System.Windows.Input.Cursors.Hand;
                        pill.MouseLeftButtonDown += (s, ev) => ShowStudentWorks(studentId);
                    }

                    StudentsList.Children.Add(pill);
                }

                MainPanel.Visibility = Visibility.Collapsed;
                SubjectPanel.Visibility = Visibility.Collapsed;
                AssignmentPanel.Visibility = Visibility.Collapsed;
                SubmissionsPanel.Visibility = Visibility.Visible;
                ModalOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void BtnBackToAssignment_Click(object sender, RoutedEventArgs e)
        {
            MainPanel.Visibility = Visibility.Collapsed;
            SubjectPanel.Visibility = Visibility.Collapsed;
            AssignmentPanel.Visibility = Visibility.Visible;
            SubmissionsPanel.Visibility = Visibility.Collapsed;
            ModalOverlay.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region МОДАЛЬНОЕ ОКНО РАБОТ УЧЕНИКА

        private void ShowStudentWorks(int studentId)
        {
            currentStudentId = studentId;

            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery(
                    @"SELECT af.id, af.file_name, af.file_path, af.grade
                      FROM assignment_files af
                      WHERE af.assignment_id = @aid AND af.student_id = @sid
                      ORDER BY af.submitted_at ASC",
                    new SqlParameter("@aid", currentAssignmentId),
                    new SqlParameter("@sid", studentId));

                ModalFilesPanel.Children.Clear();

                foreach (DataRow row in dt.Rows)
                {
                    int fileId = Convert.ToInt32(row["id"]);
                    string fileName = row["file_name"].ToString();
                    string filePath = row["file_path"].ToString();
                    int grade = Convert.ToInt32(row["grade"]);

                    string ext = Path.GetExtension(fileName).ToLower();
                    string icon = "📄";
                    if (ext == ".pdf") icon = "";
                    else if (ext == ".docx" || ext == ".doc") icon = "";
                    else if (ext == ".jpg" || ext == ".png") icon = "🖼️";
                    else if (ext == ".xlsx" || ext == ".xls") icon = "📗";
                    else if (ext == ".pptx" || ext == ".ppt") icon = "📙";
                    else if (ext == ".zip" || ext == ".rar") icon = "📦";

                    var fileCard = new Border
                    {
                        Width = 80,
                        Height = 80,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                        CornerRadius = new CornerRadius(10),
                        Margin = new Thickness(5),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

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
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666"))
                    });
                    fileCard.Child = stack;

                    fileCard.MouseLeftButtonDown += (s, ev) => DownloadFile(filePath);

                    ModalFilesPanel.Children.Add(fileCard);

                    TxtModalGrade.Text = grade.ToString();
                    TxtNewGrade.Text = "";
                    currentFileId = fileId;
                }

                ModalOverlay.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void DownloadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Файл не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = Path.GetFileName(filePath);
            sfd.Filter = "Все файлы (*.*)|*.*";

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    File.Copy(filePath, sfd.FileName, true);
                    MessageBox.Show("Файл скачан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при скачивании: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSaveGrade_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtNewGrade.Text))
                {
                    MessageBox.Show("Введите оценку!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int newGrade = Convert.ToInt32(TxtNewGrade.Text.Trim());
                if (newGrade < 0 || newGrade > 5)
                {
                    MessageBox.Show("Оценка должна быть от 0 до 5!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DatabaseHelper.ExecuteNonQuery(
                    "UPDATE assignment_files SET grade = @grade WHERE id = @id",
                    new SqlParameter("@grade", newGrade),
                    new SqlParameter("@id", currentFileId));

                TxtModalGrade.Text = newGrade.ToString();
                TxtNewGrade.Clear();
                MessageBox.Show("Оценка сохранена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show("Введите корректное число!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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