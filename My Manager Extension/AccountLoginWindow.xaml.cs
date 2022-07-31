using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Windows;

namespace My_Manager_Extension
{
    /// <summary>
    /// アカウントログインウィンドウ
    /// 機能：接続するサービスの設定及び認証
    /// （AccountLoginWindow.xaml の相互作用ロジック）
    /// </summary>
    public partial class AccountLoginWindow : Window
    {
        public AccountLoginWindow(bool relogin = false)
        {
            InitializeComponent();

            this.relogin = relogin;
        }

        // 再ログインモードかどうか
        bool relogin;

        // ログインプログラムの進行状況
        int step = 0;

        // 接続するサービスのアドレス
        string[] server_address = Array.Empty<string>();

        // WebView2のユーザーデータフォルダのパス
        string user_data_folder = string.Empty;

        /// <summary>
        /// ウィンドウが表示された際の処理
        /// </summary>
        private async void Window_Initialized(object sender, EventArgs e)
        {
            // WebView2のユーザーデータフォルダをTempフォルダに作成する
            user_data_folder = Path.Combine(Path.GetTempPath(), Settings.assembly_name, Path.GetRandomFileName());
            if (Directory.Exists(user_data_folder))
            {
                Directory.Delete(user_data_folder, recursive: true);
            }
            CoreWebView2Environment webview2_environment = await CoreWebView2Environment.CreateAsync(null, user_data_folder);
            // WebView2を初期化する
            await webview2.EnsureCoreWebView2Async(webview2_environment);
            // WebView2の機能を制限する
            webview2.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            webview2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webview2.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            webview2.CoreWebView2.Settings.IsStatusBarEnabled = false;
            // 再ログインモードの場合
            if (relogin == true)
            {
                string settings_server_address = Settings.Decrypt_string((string)((Dictionary<string, object>)((Dictionary<string, object>)Settings.settings["data"])["connected_service"])["server_address"]);
                if (settings_server_address == null)
                {
                    // 設定データが壊れている場合に再ログインモードを無効にする
                    relogin = false;
                }
                else
                {
                    // ステップ1を省略する（サーバーアドレスには既に設定された値を使用する）
                    Uri target_page_uri = new Uri(settings_server_address);
                    server_address = new string[] { target_page_uri.GetLeftPart(UriPartial.Scheme), target_page_uri.Authority };
                    step = 1;
                    // ステップ2へ進む
                    webview2.CoreWebView2.Navigate(string.Join("", server_address) + "/accounts/issue_tokens/");
                    return;
                }
            }
            // ステップ1へ進む
            string html_content = Functions.GetTextResources("AccountLoginWindowData/destination_input_page.html");
            webview2.NavigateToString(html_content);
        }

        /// <summary>
        /// ウィンドウが閉じられた際の処理
        /// </summary>
        private async void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                // WebView2を破棄して、ブラウザのプロセスが終了するまで待機する
                uint wvProcessId = webview2.CoreWebView2.BrowserProcessId;
                int timeout = 10;
                webview2.Dispose();
                try
                {
                    while (Process.GetProcessById(Convert.ToInt32(wvProcessId)) != null && timeout < 1000 * 3)
                    {
                        await Task.Delay(10);
                        timeout += 10;
                    }
                }
                catch (ArgumentException) { }
                // WebView2のユーザーデータフォルダを削除する
                Directory.Delete(user_data_folder, recursive: true);
                Directory.Delete(Directory.GetParent(user_data_folder).FullName, recursive: false);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>
        /// WebView2でページ移動が始まった際の処理
        /// </summary>
        private void Webview2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Uri target_page_uri = new Uri(e.Uri);
            switch (step)
            {
                case 0:
                    // ステップ1の場合
                    if (target_page_uri.Host == "_")
                    {
                        // サーバーアドレスが入力された場合にそのアドレスに接続する（ステップ2へ進む）
                        server_address = new string[] { "http://", new Uri("http://" + HttpUtility.ParseQueryString(target_page_uri.Query).Get("server_address")).Authority };
                        step = 1;
                        webview2.CoreWebView2.Navigate(string.Join("", server_address) + "/accounts/issue_tokens/");
                    }
                    break;
                case 1:
                    // ステップ2の場合
                    if (target_page_uri.Host == "_")
                    {
                        // 「次に進む」ボタンが2回以上押された場合に適切でないアドレスに接続してしまうため、再度接続先を指定する
                        webview2.CoreWebView2.Navigate(string.Join("", server_address) + "/accounts/issue_tokens/");
                        return;
                    }
                    if (target_page_uri.Authority == server_address[1] && target_page_uri.AbsolutePath == "/accounts/issue_tokens/")
                    {
                        if (HttpUtility.ParseQueryString(target_page_uri.Query).Get("finished") != null)
                        {
                            // トークン発行ページでの操作を終えた場合
                            if (HttpUtility.ParseQueryString(target_page_uri.Query).Get("finished") == "1")
                            {
                                // トークンが発行された場合にサービスの情報を保存する
                                Dictionary<string, object> settings = Settings.settings;
                                if (relogin == false)
                                {
                                    // 再ログインモードでない場合のみサーバーアドレスを保存する
                                    ((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["server_address"] = Settings.Encrypt_string(target_page_uri.GetLeftPart(UriPartial.Scheme) + server_address[1]);
                                }
                                ((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["access_token"] = Settings.Encrypt_string(HttpUtility.ParseQueryString(target_page_uri.Query).Get("access"));
                                ((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["refresh_token"] = Settings.Encrypt_string(HttpUtility.ParseQueryString(target_page_uri.Query).Get("refresh"));
                                Settings.settings = settings;
                                try
                                {
                                    this.DialogResult = true;
                                }
                                catch (InvalidOperationException) { }
                            }
                            this.Close();
                        }
                    }
                    break;
            }
        }
    }
}
