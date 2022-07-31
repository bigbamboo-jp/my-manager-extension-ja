using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using Windows.Foundation.Collections;

namespace My_Manager_Extension
{
    /// <summary>
    /// アプリケーション全体で使用する機能を集約したクラス
    /// </summary>
    internal class Functions
    {
        /// <summary>
        /// トースト通知を表示するメソッド
        /// </summary>
        internal static void SendToastNotification(int situation)
        {
            ToastContentBuilder toast_notification = null;
            DateTimeOffset? ExpirationTime = null;
            switch (situation)
            {
                case 1:
                    // 認証済み、一度も出席を記録していない
                    toast_notification = new ToastContentBuilder()
                        .AddText("出席を記録する")
                        .AddText("まだ出席の記録を行っていないようです。")
                        .AddText("この通知をクリックすると出席を記録するページを表示します。")
                        .AddArgument("action", "show_entry_page")
                        .AddButton(
                            new ToastButton().SetContent("通知設定を開く")
                            .AddArgument("action", "show_notification_settings"));
                    ExpirationTime = new DateTimeOffset(DateTime.Now.AddDays(1).Date);
                    break;
                case 2:
                    // 認証済み、連続作業時間がユーザーが設定した値以上になった
                    toast_notification = new ToastContentBuilder()
                        .AddText("休憩リマインダー")
                        .AddText("連続して作業している時間が" + TimeSpan.FromSeconds((int)((Dictionary<string, object>)Settings.settings["data"])["rest_reminder_waiting_time"]).TotalHours.ToString() + "時間を超えました。そろそろ休憩を取りましょう！")
                        .AddText("この通知をクリックすると退席を記録するページを表示します。")
                        .AddArgument("action", "show_leave_page")
                        .AddButton(
                            new ToastButton().SetContent("通知設定を開く")
                            .AddArgument("action", "show_notification_settings"));
                    ExpirationTime = new DateTimeOffset(DateTime.Now.AddDays(1).Date);
                    break;
                case 3:
                    // サービス設定は完了しているが、認証情報が無効化されている
                    toast_notification = new ToastContentBuilder()
                        .AddText("再ログインが必要です")
                        .AddText("認証情報が無効化されているため、サービスへのログインに失敗しました。")
                        .AddText("出席時の記録などのリマインダーを受け取るには、この通知をクリックして再ログインを行ってください。")
                        .AddArgument("action", "show_account_relogin_window")
                        .AddButton(
                            new ToastButton().SetContent("通知設定を開く")
                            .AddArgument("action", "show_notification_settings"));
                    break;
                case 10:
                    // サービス設定が完了していない
                    toast_notification = new ToastContentBuilder()
                        .AddText("サービスにログインする")
                        .AddText("使用しているサービスにログインすると、出席時の記録などのリマインダーを受け取ることができます。")
                        .AddText("この通知をクリックするとサービス設定を表示します。")
                        .AddArgument("action", "show_account_login_window")
                        .AddButton(
                            new ToastButton().SetContent("通知設定を開く")
                            .AddArgument("action", "show_notification_settings"));
                    break;
                case 11:
                    // サービスへの接続に失敗した
                    toast_notification = new ToastContentBuilder()
                        .AddText("サービスに接続できません")
                        .AddText("現在サービスに接続できないため、出席状況等を確認できません。")
                        .AddText("この通知をクリックするとサービスのホームページを表示します。")
                        .AddArgument("action", "show_home_page")
                        .AddButton(
                            new ToastButton().SetContent("通知設定を開く")
                            .AddArgument("action", "show_notification_settings"));
                    break;
                case 12:
                    // 設定データが破損している
                    toast_notification = new ToastContentBuilder()
                        .AddText("再ログインが必要です")
                        .AddText("認証情報が読み込めないため、サービスにログインできません。")
                        .AddText("出席時の記録などのリマインダーを受け取るには、この通知をクリックして再ログインを行ってください。")
                        .AddArgument("action", "show_account_relogin_window")
                        .AddButton(
                            new ToastButton().SetContent("通知設定を開く")
                            .AddArgument("action", "show_notification_settings"));
                    break;
            }
            if (toast_notification != null)
            {
                toast_notification.Show(toast =>
                {
                    toast.ExpirationTime = ExpirationTime;
                });
            }
        }

        /// <summary>
        /// クリックされたトースト通知に設定されているアクションを実行するメソッド
        /// </summary>
        internal static void HandleActionsOnToastNotifications(Dictionary<string, string> arguments)
        {
            if (arguments.ContainsKey("action"))
            {
                // action引数に応じて処理をする
                switch (arguments["action"])
                {
                    case "":
                        // アクション未設定
                        break;
                    case "show_notification_settings":
                        // 通知設定を表示する
                        SettingsWindow_Show(tab_control_selected_index: 0);
                        break;
                    case "show_account_login_window":
                        // アカウントログインウィンドウ（通常ログインモード）を表示する
                        new AccountLoginWindow(relogin: false).ShowDialog();
                        break;
                    case "show_account_relogin_window":
                        // アカウントログインウィンドウ（再ログインモード）を表示する
                        new AccountLoginWindow(relogin: true).ShowDialog();
                        break;
                    case "show_home_page":
                        // サービスのホームページを開く
                        OpenServicePage(target_number: 0);
                        break;
                    case "show_entry_page":
                        // サービスの出席記録ページを開く
                        OpenServicePage(target_number: 1);
                        break;
                    case "show_leave_page":
                        // サービスの退席記録ページを開く
                        OpenServicePage(target_number: 2);
                        break;
                }
            }
        }

        /// <summary>
        /// トースト通知に対する操作を購読する
        /// ソース：https://docs.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/send-local-toast?tabs=desktop#step-3-handling-activation
        /// </summary>
        internal static void StartListeningToNotificationActivation()
        {
            // Listen to notification activation
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                // Obtain the arguments from the notification
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                // Obtain any user input (text boxes, menu selections) from the notification
                ValueSet userInput = toastArgs.UserInput;

                // Need to dispatch to UI thread if performing UI operations
                MainWindow.mainwindow.Dispatcher.Invoke(delegate
                {
                    Dictionary<string, string> arguments = new Dictionary<string, string>();
                    string[] toastArgs_Argument = toastArgs.Argument.Split(";");
                    if (toastArgs_Argument.SequenceEqual(new string[] { "" }) == false)
                    {
                        foreach (string arg in toastArgs_Argument)
                        {
                            string[] arg_splitted = arg.Split("=");
                            arguments[arg_splitted[0]] = arg_splitted[1];
                        }
                    }
                    HandleActionsOnToastNotifications(arguments);
                });
            };
        }

        /// <summary>
        /// トースト通知に対する操作の購読をやめる
        /// ソース：https://docs.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/send-local-toast?tabs=desktop#step-4-handling-uninstallation
        /// </summary>
        internal static void StopListeningToNotificationActivation()
        {
            // Stop listening to notification activation
            ToastNotificationManagerCompat.Uninstall();
        }

        /// <summary>
        /// ビルドアクションが「リソース」に設定されているテキストファイルのデータを取得するメソッド
        /// </summary>
        internal static string GetTextResources(string resource_path)
        {
            // resource_path 指定例：Texts/Terms.txt
            Uri path = new Uri("pack://application:,,,/" + resource_path, UriKind.Absolute);
            StreamResourceInfo resource = Application.GetResourceStream(path);
            using (var sr = new StreamReader(resource.Stream))
            {
                return sr.ReadToEnd();
            }
        }

        /// <summary>
        /// APIの認証状態を確認するメソッド
        /// </summary>
        internal static async Task<int> ConfirmAPIAuthorizationState()
        {
            Dictionary<string, object> settings = Settings.settings;
            string server_address = (string)((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["server_address"];
            if (server_address == string.Empty)
            {
                // APIの設定が完了していない場合
                return 10;
            }
            else
            {
                // APIの設定が完了している場合
                if (Settings.Decrypt_string(server_address) == null)
                {
                    // 設定データが破損している場合
                    return 12;
                }
                string refresh_token = Settings.Decrypt_string((string)((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["refresh_token"]);
                if (refresh_token == null)
                {
                    // 設定データが破損している場合
                    return 12;
                }
                // リフレッシュトークンが有効であるか確認する
                return await VerifyAPITokens(refresh_token);
            }
        }

        // [API情報（要更新）]
        // APIのバージョン：v1
        // ※APIエンドポイントはrequest_url変数を宣言する際のコードに含めている

        // httpクライアント
        internal static HttpClient http_client = null;

        /// <summary>
        /// サービスの名前を取得するメソッド
        /// </summary>
        internal static async Task<string> GetServiceName()
        {
            if (http_client == null)
            {
                http_client = new HttpClient();
            }
            string server_address = Settings.Decrypt_string((string)((Dictionary<string, object>)((Dictionary<string, object>)Settings.settings["data"])["connected_service"])["server_address"]);
            if (server_address == null)
            {
                // 設定データが破損している場合
                return null;
            }
            string request_url = server_address + "/api/v1/site/information/";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, request_url);
            try
            {
                HttpResponseMessage response = await http_client.SendAsync(request);
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        // 正常にリクエストが完了した場合
                        string json_content = await response.Content.ReadAsStringAsync();
                        Dictionary<string, object> content = (Dictionary<string, object>)JsonConvert_DeserializeObject(JToken.Parse(json_content));
                        return (string)content["name"];
                    default:
                        // サーバーのステータス等が原因でリクエストが失敗した場合
                        return null;
                }
            }
            catch (HttpRequestException ex)
            {
                // 通信エラーが発生した場合
                // throw ex;
                return null;
            }
        }

        /// <summary>
        /// APIトークンが有効であるか確認するメソッド
        /// </summary>
        internal static async Task<int> VerifyAPITokens(string token)
        {
            if (http_client == null)
            {
                http_client = new HttpClient();
            }
            string server_address = Settings.Decrypt_string((string)((Dictionary<string, object>)((Dictionary<string, object>)Settings.settings["data"])["connected_service"])["server_address"]);
            if (server_address == null)
            {
                // 設定データが破損している場合
                return 12;
            }
            string request_url = server_address + "/api/v1/token/verify/";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, request_url);
            Dictionary<string, object> parameters = new Dictionary<string, object>()
            {
                ["token"] = token
            };
            string parameters_json = JsonConvert.SerializeObject(parameters, Formatting.Indented);
            request.Content = new StringContent(parameters_json, Encoding.UTF8, "application/json");
            try
            {
                HttpResponseMessage response = await http_client.SendAsync(request);
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        // 正常にリクエストが完了した場合
                        return 0;
                    case System.Net.HttpStatusCode.Unauthorized:
                        // ユーザー認証の問題でリクエストが失敗した場合
                        return 3;
                    default:
                        // サーバーのステータス等が原因でリクエストが失敗した場合
                        return 11;
                }
            }
            catch (HttpRequestException ex)
            {
                // 通信エラーが発生した場合
                // throw ex;
                return 11;
            }
        }

        /// <summary>
        /// APIトークンを更新するメソッド
        /// </summary>
        internal static async Task<int> RefreshAPITokens()
        {
            if (http_client == null)
            {
                http_client = new HttpClient();
            }
            Dictionary<string, object> settings = Settings.settings;
            string server_address = Settings.Decrypt_string((string)((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["server_address"]);
            if (server_address == null)
            {
                // 設定データが破損している場合
                return 12;
            }
            string request_url = server_address + "/api/v1/token/refresh/";
            string refresh_token = Settings.Decrypt_string((string)((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["refresh_token"]);
            if (refresh_token == null)
            {
                // 設定データが破損している場合
                return 12;
            }
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, request_url);
            Dictionary<string, object> parameters = new Dictionary<string, object>()
            {
                ["refresh"] = refresh_token
            };
            string parameters_json = JsonConvert.SerializeObject(parameters, Formatting.Indented);
            request.Content = new StringContent(parameters_json, Encoding.UTF8, "application/json");
            try
            {
                HttpResponseMessage response = await http_client.SendAsync(request);
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        // 正常にリクエストが完了した場合
                        string json_content = await response.Content.ReadAsStringAsync();
                        Dictionary<string, object> content = (Dictionary<string, object>)JsonConvert_DeserializeObject(JToken.Parse(json_content));
                        ((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["access_token"] = Settings.Encrypt_string((string)content["access"]);
                        ((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["refresh_token"] = Settings.Encrypt_string((string)content["refresh"]);
                        Settings.settings = settings;
                        return 0;
                    case System.Net.HttpStatusCode.Unauthorized:
                        // ユーザー認証の問題でリクエストが失敗した場合
                        return 3;
                    default:
                        // サーバーのステータス等が原因でリクエストが失敗した場合
                        return 11;
                }
            }
            catch (HttpRequestException ex)
            {
                // 通信エラーが発生した場合
                // throw ex;
                return 11;
            }
        }

        /// <summary>
        /// サービスからユーザーのプロフィールを取得するメソッド
        /// </summary>
        internal static async Task<Dictionary<string, object>> GetUserProfile()
        {
            if (http_client == null)
            {
                http_client = new HttpClient();
            }
            Dictionary<string, object> settings = Settings.settings;
            string server_address = Settings.Decrypt_string((string)((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["server_address"]);
            if (server_address == null)
            {
                // 設定データが破損している場合
                return null;
            }
            string request_url = server_address + "/api/v1/user/profile/";
            string access_token = Settings.Decrypt_string((string)((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["access_token"]);
            if (access_token == null)
            {
                // 設定データが破損している場合
                return null;
            }
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, request_url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            try
            {
                HttpResponseMessage response = await http_client.SendAsync(request);
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        // 正常にリクエストが完了した場合
                        string json_content = await response.Content.ReadAsStringAsync();
                        return (Dictionary<string, object>)JsonConvert_DeserializeObject(JToken.Parse(json_content));
                    case System.Net.HttpStatusCode.Unauthorized:
                        // ユーザー認証の問題でリクエストが失敗した場合
                        if (await RefreshAPITokens() == 0)
                        {
                            // APIトークンの更新に成功した場合
                            return await GetUserProfile();
                        }
                        else
                        {
                            // APIトークンの更新に失敗した場合
                            return null;
                        }
                    default:
                        // サーバーのステータス等が原因でリクエストが失敗した場合
                        return null;
                }
            }
            catch (HttpRequestException ex)
            {
                // 通信エラーが発生した場合
                // throw ex;
                return null;
            }
        }

        /// <summary>
        /// サービスから今日のユーザーの出席状況を取得するメソッド
        /// </summary>
        internal static async Task<Dictionary<string, object>> GetTodaysEntryStatus()
        {
            if (http_client == null)
            {
                http_client = new HttpClient();
            }
            Dictionary<string, object> settings = Settings.settings;
            string server_address = Settings.Decrypt_string((string)((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["server_address"]);
            if (server_address == null)
            {
                // 設定データが破損している場合
                return null;
            }
            string request_url = server_address + "/api/v1/user/todays_entry_status/";
            string access_token = Settings.Decrypt_string((string)((Dictionary<string, object>)((Dictionary<string, object>)settings["data"])["connected_service"])["access_token"]);
            if (access_token == null)
            {
                // 設定データが破損している場合
                return null;
            }
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, request_url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            try
            {
                HttpResponseMessage response = await http_client.SendAsync(request);
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        // 正常にリクエストが完了した場合
                        string json_content = await response.Content.ReadAsStringAsync();
                        return (Dictionary<string, object>)JsonConvert_DeserializeObject(JToken.Parse(json_content));
                    case System.Net.HttpStatusCode.Unauthorized:
                        // ユーザー認証の問題でリクエストが失敗した場合
                        if (await RefreshAPITokens() == 0)
                        {
                            // APIトークンの更新に成功した場合
                            return await GetTodaysEntryStatus();
                        }
                        else
                        {
                            // APIトークンの更新に失敗した場合
                            return null;
                        }
                    default:
                        // サーバーのステータス等が原因でリクエストが失敗した場合
                        return null;
                }
            }
            catch (HttpRequestException ex)
            {
                // 通信エラーが発生した場合
                // throw ex;
                return null;
            }
        }

        /// <summary>
        /// サービスの各ページを開くメソッド
        /// </summary>
        internal static void OpenServicePage(int target_number)
        {
            string server_address = Settings.Decrypt_string((string)((Dictionary<string, object>)((Dictionary<string, object>)Settings.settings["data"])["connected_service"])["server_address"]);
            if (server_address == null)
            {
                // 設定データが破損している場合
                return;
            }
            string page_url = server_address;
            switch (target_number)
            {
                case 0:
                    // サービスのホームページ
                    page_url += "";
                    break;
                case 1:
                    // サービスの出席記録ページ
                    page_url += "/s/entry_description";
                    break;
                case 2:
                    // サービスの退席記録ページ
                    page_url += "/s/leave_description";
                    break;
            }
            // システム既定のウェブブラウザでページを開く
            AccessWebsite(page_url);
        }

        /// <summary>
        /// JSON形式のデータを再帰的にデシリアライズするメソッド
        /// </summary>
        internal static object JsonConvert_DeserializeObject(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    // 最深層まで再帰処理でデータを取り出す
                    return token.Children<JProperty>()
                                .ToDictionary(prop => prop.Name,
                                              prop => JsonConvert_DeserializeObject(prop.Value));

                case JTokenType.Array:
                    return token.Select(JsonConvert_DeserializeObject).ToList();

                case JTokenType.Integer:
                    // 数値は可能な限りInt32型で返す
                    try
                    {
                        return Convert.ToInt32(((JValue)token).Value); // int
                    }
                    catch (OverflowException)
                    {
                        // データがInt32型の最大値を超える場合はそのまま（Int64型）で返す
                        return ((JValue)token).Value; // long
                    }

                default:
                    return ((JValue)token).Value;
            }
        }

        /// <summary>
        /// アプリケーションを再起動するメソッド
        /// </summary>
        internal static void RestartApplication(bool run_as = false)
        {
            // 移行先のアプリケーションを起動する
            string args = string.Join("\" \"", Environment.GetCommandLineArgs().Skip(1));
            if (args != string.Empty)
            {
                args = "\"" + args + "\"";
            }
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                Arguments = args,
                UseShellExecute = true
            };
            if (run_as == true)
            {
                // 指定された場合に管理者権限で起動する
                psi.Verb = "RunAs";
            }
            try
            {
                Process.Start(psi);
            }
            catch (System.ComponentModel.Win32Exception) { }
            // 移行元のアプリケーションを終了する（移行先を起動できなかった場合でも行う）
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 既定のウェブブラウザでURLを開くメソッド
        /// </summary>
        public static void AccessWebsite(string url)
        {
            // URLの末尾に半角スペースを追加する（一部のURLにおいてそうしないと動かない）
            url += " ";

            // OSコマンドインジェクション（CWE 78）対策
            // 参考：https://www.veracode.com/security/dotnet/cwe-78
            Regex valDesc = new Regex(@"[a-zA-Z0-9\x20]+$");
            if (!valDesc.IsMatch(url))
            {
                return;
            }

            // 既定のウェブブラウザでURLを開く
            Process.Start(new ProcessStartInfo("cmd", string.Format("/c start {0}", url)) { CreateNoWindow = true });
        }

        // 設定ウィンドウのインスタンス
        public static Window settings_window;

        /// <summary>
        /// 設定ウィンドウを表示するメソッド
        /// </summary>
        public static void SettingsWindow_Show(int? tab_control_selected_index = null)
        {
            if (settings_window != null)
            {
                if (settings_window.IsLoaded == true)
                {
                    // 既に表示している場合は新しく表示せず、既存のウィンドウを最前面に移動する
                    if (tab_control_selected_index != null)
                    {
                        // 表示するタブが指定されている場合に、表示しているタブを変更する
                        ((SettingsWindow)settings_window).tab_control.SelectedIndex = (int)tab_control_selected_index;
                    }
                    settings_window.Activate();
                    return;
                }
            }
            settings_window = new SettingsWindow();
            if (tab_control_selected_index != null)
            {
                // 表示するタブが指定されている場合に、はじめに表示するタブを変更する
                ((SettingsWindow)settings_window).tab_control.SelectedIndex = (int)tab_control_selected_index;
            }
            settings_window.Show();
            settings_window.Activate();
        }

        /// <summary>
        /// 選択できる言語のリストを提供するメソッド
        /// </summary>
        public static Dictionary<string, string> GetAvailableLanguages()
        {
            System.Collections.SortedList available_languages = (System.Collections.SortedList)Application.Current.FindResource("general/available_languages");
            // SortedListをDictionaryに変換して返す
            return available_languages.Keys.Cast<string>().ToDictionary(x => x, x => (string)available_languages[x]);
        }

        // 言語情報ファイルのパス
        public const string LANGUAGE_INFORMATION_FILE_PATH = "pack://application:,,,/Languages/LanguageInformation.xaml";
        // 言語データファイルのパス
        public const string LANGUAGE_DATA_FILE_PATH = "pack://application:,,,/Languages/{0}.xaml";
        // デフォルト言語のリージョンタグ
        public const string DEFAULT_LANGUAGE_SETTINGS = "en-US";

        /// <summary>
        /// ユーザーが設定した言語の言語データを読み込むメソッド
        /// </summary>
        public static void LoadLanguageData()
        {
            // 言語情報を読み込む
            ResourceDictionary language_information_resource_dictionary = new ResourceDictionary
            {
                Source = new Uri(LANGUAGE_INFORMATION_FILE_PATH, UriKind.Absolute)
            };
            Application.Current.Resources.MergedDictionaries.Add(language_information_resource_dictionary);
            const string DEFAULT_LANGUAGE = "en-US"; // ユーザーが設定した言語と共に読み込む言語（ユーザーが設定した言語のデータに不足があった場合に使用される）
            // デフォルト言語のデータを読み込む
            ResourceDictionary default_language_resource_dictionary = new ResourceDictionary
            {
                Source = new Uri(string.Format(LANGUAGE_DATA_FILE_PATH, DEFAULT_LANGUAGE), UriKind.Absolute)
            };
            Application.Current.Resources.MergedDictionaries.Add(default_language_resource_dictionary);
            string application_language_setting;
            if (File.Exists(Settings.setting_file_path) == true)
            {
                // 設定ファイルが存在する場合
                application_language_setting = (string)((Dictionary<string, object>)Settings.settings["data"])["language"];
            }
            else
            {
                // 設定ファイルが存在しない場合
                application_language_setting = string.Empty;
            }
            bool change_application_language_setting = false;
            if (application_language_setting == string.Empty)
            {
                // アプリケーションで使用する言語が設定されていない場合
                CultureInfo system_language = CultureInfo.CurrentCulture;
                Dictionary<string, string> available_languages = GetAvailableLanguages();
                if (available_languages.ContainsKey(system_language.Name))
                {
                    // システムの言語がアプリケーションの言語として選択できる場合
                    application_language_setting = system_language.Name;
                }
                else
                {
                    // システムの言語がアプリケーションの言語として選択できない場合
                    application_language_setting = DEFAULT_LANGUAGE_SETTINGS;
                }
                change_application_language_setting = true;
            }
            if (application_language_setting != DEFAULT_LANGUAGE)
            {
                // ユーザーが設定した言語とデフォルトの言語が異なる場合
                ResourceDictionary language_resource_dictionary = new ResourceDictionary
                {
                    Source = new Uri(string.Format(LANGUAGE_DATA_FILE_PATH, application_language_setting), UriKind.Absolute)
                };
                // デフォルト言語のデータをユーザーが設定した言語のデータで置き換える（双方に存在するリソースのみ）
                Application.Current.Resources.MergedDictionaries.Add(language_resource_dictionary);
            }
            if (change_application_language_setting == true)
            {
                // アプリケーションで使用する言語が変更された場合
                Dictionary<string, object> settings = Settings.settings;
                ((Dictionary<string, object>)settings["data"])["language"] = application_language_setting;
                Settings.settings = settings;
            }
        }
    }
}
