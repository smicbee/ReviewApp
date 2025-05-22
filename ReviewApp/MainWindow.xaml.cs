using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Web.WebView2.Core;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReviewApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string originalHtml = "Das ist ein <b>Test</b>.";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await EditorView.EnsureCoreWebView2Async();
            await DiffView.EnsureCoreWebView2Async();

            // Setup: Empfange Editor-Inhalt per WebView2-PostMessage
            EditorView.CoreWebView2.WebMessageReceived += EditorContentChanged;

            // Lade Editor HTML als String
            EditorView.NavigateToString(editorHtml);

            // Starte mit Initial-Diff
            UpdateDiff(originalHtml);

            LoadingOverlay.Visibility = Visibility.Visible;
            for (int i = 0; i < 10; i++)
            {
                AddImageComment(@"B:\Google Drive\Desktop\image.jpg", "Image Caption #" + i.ToString());
            }
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        public class ImageComment
        {
            public string ImagePath { get; set; }
            public string OriginalCaption { get; set; }
        }

        private void EditorContentChanged(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string newHtml = e.TryGetWebMessageAsString();
            UpdateDiff(newHtml);
        }

        private void UpdateDiff(string editedHtml)
        {
            var differ = new HtmlDiff.HtmlDiff(originalHtml, editedHtml);
            string diffHtml = differ.Build();

            string htmlWithStyles = $@"
                <html>
                <head>
                  <style>
                    del {{ background-color: #ffcccc; text-decoration: line-through; }}
                    ins {{ background-color: #ccffcc; text-decoration: none; }}
                    body {{ font-family: sans-serif; padding: 10px; }}
                  </style>
                </head>
                <body>{diffHtml}</body>
                </html>";

            DiffView.NavigateToString(htmlWithStyles);
        }

        private string editorHtml = @"
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset='utf-8'>
          <style>
            body { font-family: sans-serif; padding: 10px; }
            #editor {
              width: 100%;
              height: 100%;
              border: 1px solid #ccc;
              padding: 10px;
              outline: none;
            }
          </style>
          <script>
            window.onload = function () {
              const editor = document.getElementById('editor');
              editor.addEventListener('input', () => {
                window.chrome.webview.postMessage(editor.innerHTML);
              });
            };
          </script>
        </head>
        <body>
          <div id='editor' contenteditable='true'>
            Das ist ein <b>Test</b>.
          </div>
        </body>
        </html>";




        private Task AddImageComment(string imagePath, string originalCaption)
        {
            int imgid;
            string original = originalCaption;

            // Hauptcontainer für die Zeile
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 10),
                VerticalAlignment = VerticalAlignment.Top
            };

            // Bild
            var image = new Image
            {
                Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute)),
                Width = 256,
                Height = 256,
                Margin = new Thickness(5)
            };

            // Originaltext speichern (lokale Variable für diese Zeile)
            // Editierbares Textfeld
            var textBox = new TextBox
            {
                Text = originalCaption,
                Width = 400,
                Height = 200,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Margin = new Thickness(5)
            };

            // SpellCheck aktivieren
            System.Windows.Controls.SpellCheck.SetIsEnabled(textBox, true);

            // Diff-Anzeige (WebView2)
            var diffView = new Microsoft.Web.WebView2.Wpf.WebView2
            {
                Width = 400,
                Height = 200,
                Margin = new Thickness(5)
            };
            diffView.EnsureCoreWebView2Async();

            var buttonWidth = 80;
            var buttonHeight = 40;
            // Buttons-Panel
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,        // horizontal nebeneinander
                HorizontalAlignment = HorizontalAlignment.Left, // links ausrichten
                VerticalAlignment = VerticalAlignment.Top,       // oben ausrichten
                Margin = new Thickness(5)
            };

            var acceptButton = new Button
            {
                Content = "✔",
                Height = buttonHeight,
                Width = buttonWidth,
                Foreground = Brushes.Green,
                FontSize = 16,
                Margin = new Thickness(0, 10, 10, 0),
                ToolTip = "Accept"
            };
            acceptButton.Click += (s, e) =>
            {
                // Änderungen übernehmen: Original auf aktuellen Text setzen
                original = textBox.Text;

                // Diff neu berechnen - jetzt keine Änderungen mehr
                var differ = new HtmlDiff.HtmlDiff(original, original);
                string diffHtml = differ.Build();

                string styledHtml = $@"
        <html>
        <head>
            <style>
                ins {{ background-color: lightgreen; text-decoration: none; }}
                del {{ background-color: lightcoral; text-decoration: none; }}
                body {{ font-family: Segoe UI, sans-serif; font-size: 14px; margin: 0; padding: 4px; }}
            </style>
        </head>
        <body>{diffHtml}</body>
        </html>";

                if (diffView.CoreWebView2 != null)
                {
                    diffView.NavigateToString(styledHtml);
                }
            };

            // Revert-Button
            var revertButton = new Button
            {
                Content = "✘",
                Width = buttonWidth,
                Height = buttonHeight,
                Foreground = Brushes.Red,
                FontSize = 16,
                Margin = new Thickness(0, 10, 10, 0),
                ToolTip = "Revert"
            };
            revertButton.Click += (s, e) =>
            {
                // Caption zurücksetzen auf Original
                textBox.Text = original;

                // Diff neu berechnen - keine Änderungen
                var differ = new HtmlDiff.HtmlDiff(original, original);
                string diffHtml = differ.Build();

                string styledHtml = $@"
        <html>
        <head>
            <style>
                ins {{ background-color: lightgreen; text-decoration: none; }}
                del {{ background-color: lightcoral; text-decoration: none; }}
                body {{ font-family: Segoe UI, sans-serif; font-size: 14px; margin: 0; padding: 4px; }}
            </style>
        </head>
        <body>{diffHtml}</body>
        </html>";

                if (diffView.CoreWebView2 != null)
                {
                    diffView.NavigateToString(styledHtml);
                }
            };

            buttonsPanel.Children.Add(acceptButton);
            buttonsPanel.Children.Add(revertButton);

            // Diff automatisch aktualisieren, wenn TextBox sich ändert
            textBox.TextChanged += (s, e) =>
            {
                var differ = new HtmlDiff.HtmlDiff(original, textBox.Text);
                string diffHtml = differ.Build();

                string styledHtml = $@"
        <html>
        <head>
            <style>
                ins {{ background-color: lightgreen; text-decoration: none; }}
                del {{ background-color: lightcoral; text-decoration: none; }}
                body {{ font-family: Segoe UI, sans-serif; font-size: 14px; margin: 0; padding: 4px; }}
            </style>
        </head>
        <body>{diffHtml}</body>
        </html>";

                if (diffView.CoreWebView2 != null)
                {
                    diffView.NavigateToString(styledHtml);
                }
            };

            // Controls zum Haupt-StackPanel hinzufügen
            row.Children.Add(image);
            row.Children.Add(textBox);
            row.Children.Add(diffView);
            row.Children.Add(buttonsPanel);

            ImageCommentPanel.Children.Add(row);

            return null;
        }

    }
}