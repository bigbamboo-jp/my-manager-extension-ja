using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace My_Manager_Extension
{
    /// <summary>
    /// 設定ウィンドウ
    /// 機能：アプリケーション設定の変更及び各機能の設定ウィンドウへのリンク
    /// （SettingsWindow.xaml の相互作用ロジック）
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ウィンドウが表示された際の処理
        /// </summary>
        private async void Window_Initialized(object sender, EventArgs e)
        {
            // ウィンドウタイトルを設定する
            this.Title = Settings.assembly_name;
            // デザイナーでの表示と実際の表示のずれを直す
            double title_bar_height_in_designer = 15.96;
            this.Height += System.Windows.Forms.SystemInformation.CaptionHeight - title_bar_height_in_designer;
            // 言語の選択肢を読み込む
            language_combo_box.ItemsSource = Functions.GetAvailableLanguages();
            // 「このアプリケーションについて」タブを読み込む
            application_name_label.Content = Settings.assembly_name;
            Assembly executing_assembly = Assembly.GetExecutingAssembly();
            version_label.Content = string.Format("Version {0}", executing_assembly.GetName().Version.ToString()); // 要ローカライズ
            AssemblyCopyrightAttribute aca = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute));
            if (aca == null)
            {
                // 著作権情報が登録されていない場合
                copyright_label.Content = string.Empty;
            }
            else
            {
                // 著作権情報が登録されている場合
                copyright_label.Content = string.Format("{0}", aca.Copyright); // 要ローカライズ
            }
            terms_text_box.Text = Functions.GetTextResources("Texts/Terms.txt");
            third_party_notification_text_box.Text = Functions.GetTextResources("Texts/ThirdPartyNotification.txt");
            // アプリケーション設定を読み込む
            Load_settings();
            await Load_service_settings();
        }

        /// <summary>
        /// ウィンドウがアクティブ化された際の処理
        /// </summary>
        private async void Window_Activated(object sender, EventArgs e)
        {
            if (service_action_button1.Tag != null)
            {
                if ((int)service_action_button1.Tag == 0)
                {
                    if (await Functions.ConfirmAPIAuthorizationState() != 10)
                    {
                        // 設定ウィンドウ以外で呼び出されたアカウントログインウィンドウによる変更を反映する
                        await Load_service_settings();
                    }
                }
            }
        }

        /// <summary>
        /// OKボタンがクリックされた際の処理
        /// </summary>
        private void Ok_button_Click(object sender, RoutedEventArgs e)
        {
            if (Save_settings() == false)
            {
                // 設定の保存が正常に完了した場合のみウィンドウを閉じる
                this.Close();
            }
        }

        /// <summary>
        /// キャンセルボタンがクリックされた際の処理
        /// </summary>
        private void Cancel_button_Click(object sender, RoutedEventArgs e)
        {
            // ウィンドウを閉じる
            this.Close();
        }

        /// <summary>
        /// アプリケーション設定（サービス設定は除く）を読み込むメソッド
        /// </summary>
        private void Load_settings()
        {
            Dictionary<string, object> settings = (Dictionary<string, object>)Settings.settings["data"];
            entry_reminder_feature.IsChecked = (bool)settings["entry_reminder_feature"];
            // 分単位に直して表示する
            entry_reminder_waiting_time.Text = ((int)settings["entry_reminder_waiting_time"] / 60).ToString();
            rest_reminder_feature.IsChecked = (bool)settings["rest_reminder_feature"];
            // 時間単位に直して表示する
            rest_reminder_waiting_time.Text = ((int)settings["rest_reminder_waiting_time"] / 60 / 60).ToString();
            initial_setting_reminder_feature.IsChecked = (bool)settings["initial_setting_reminder_feature"];
            // KeyValuePair(言語タグ, 言語名)
            language_combo_box.SelectedItem = new KeyValuePair<string, string>((string)settings["language"], Functions.GetAvailableLanguages()[(string)settings["language"]]);
        }

        /// <summary>
        /// サービス設定を読み込むメソッド
        /// </summary>
        private async Task Load_service_settings()
        {
            // 読み込み中の表示を出す
            service_information_label.Content = "読み込み中..."; // 要ローカライズ
            service_server_address_label.Content = string.Empty;
            service_login_user_label.Content = string.Empty;
            service_action_button1.Visibility = Visibility.Hidden;
            service_action_button2.Visibility = Visibility.Hidden;
            // APIの認証状況を確認して、それに応じて処理をする
            int api_authorization_state = await Functions.ConfirmAPIAuthorizationState();
            if (api_authorization_state == 10)
            {
                // サービスにログインしていない場合
                service_information_label.Content = "ログインしていません"; // 要ローカライズ
                service_server_address_label.Content = string.Empty;
                service_login_user_label.Content = string.Empty;
                service_action_button1.Visibility = Visibility.Visible;
                service_action_button1.Tag = 0;
                service_action_button1.Content = "ログインする"; // 要ローカライズ
                service_action_button2.Visibility = Visibility.Hidden;
                service_action_button2.Tag = -1;
                service_action_button2.Content = string.Empty;
            }
            else
            {
                // サービスにログインしている場合
                string service_name = await Functions.GetServiceName();
                if (service_name == null)
                {
                    service_name = "(取得できません)"; // 要ローカライズ
                }
                string server_address = Settings.Decrypt_string((string)((Dictionary<string, object>)((Dictionary<string, object>)Settings.settings["data"])["connected_service"])["server_address"]);
                if (server_address == null)
                {
                    server_address = "(読み込めません)"; // 要ローカライズ
                }
                string user_full_name;
                Dictionary<string, object> user_profile = await Functions.GetUserProfile();
                if (user_profile == null)
                {
                    user_full_name = "(取得できません)"; // 要ローカライズ
                }
                else
                {
                    if ((string)user_profile["first_name"] == string.Empty && (string)user_profile["last_name"] == string.Empty)
                    {
                        // 名前が設定されていない場合
                        user_full_name = (string)user_profile["username"];
                    }
                    else
                    {
                        // 名前が設定されている場合
                        string user_full_name_template = ((string)user_profile["full_name_template"]).Replace("${first_name}", "{0}").Replace("${last_name}", "{1}");
                        user_full_name = string.Format(user_full_name_template, (string)user_profile["first_name"], (string)user_profile["last_name"]).Trim();
                    }
                }
                service_information_label.Content = "サービス名: " + service_name; // 要ローカライズ
                if (server_address.StartsWith("http") == true)
                {
                    // サーバーアドレスが読み込めた場合
                    service_server_address_label.Content = "サーバーアドレス: " + "(" + new Uri(server_address).Scheme + ") " + new Uri(server_address).Authority; // 要ローカライズ
                }
                else
                {
                    // サーバーアドレスが読み込めなかった場合
                    service_server_address_label.Content = "サーバーアドレス: " + server_address; // 要ローカライズ
                }
                service_login_user_label.Content = "ログインしているユーザー: " + user_full_name; // 要ローカライズ
                service_action_button1.Visibility = Visibility.Visible;
                service_action_button1.Tag = 1;
                service_action_button1.Content = "再ログインする"; // 要ローカライズ
                service_action_button2.Visibility = Visibility.Visible;
                service_action_button2.Tag = 0;
                service_action_button2.Content = "設定を消去する"; // 要ローカライズ
            }
        }

        /// <summary>
        /// アプリケーション設定（サービス設定は除く）を保存するメソッド
        /// </summary>
        private bool Save_settings()
        {
            Dictionary<string, object> settings = Settings.settings;
            ((Dictionary<string, object>)settings["data"])["entry_reminder_feature"] = entry_reminder_feature.IsChecked ?? false;
            if (int.TryParse(entry_reminder_waiting_time.Text, out int entry_reminder_waiting_time_value) == false)
            {
                MessageBox.Show("「出席の記録についてのリマインダー｜通知を出すまでの待機時間」の入力値が不正です。", "エラー"); // 要ローカライズ
                return true;
            }
            if (entry_reminder_waiting_time_value * 60 > 60 * 60 * 24)
            {
                MessageBox.Show("「出席の記録についてのリマインダー｜通知を出すまでの待機時間」の入力値が設定できる最大値（24時間）を超えています。", "エラー"); // 要ローカライズ
                return true;
            }
            // 秒単位に直して保存する
            ((Dictionary<string, object>)settings["data"])["entry_reminder_waiting_time"] = entry_reminder_waiting_time_value * 60;
            ((Dictionary<string, object>)settings["data"])["rest_reminder_feature"] = rest_reminder_feature.IsChecked ?? false;
            if (int.TryParse(rest_reminder_waiting_time.Text, out int rest_reminder_waiting_time_value) == false)
            {
                MessageBox.Show("「休憩リマインダー｜通知を出すまでの待機時間」の入力値が不正です。", "エラー"); // 要ローカライズ
                return true;
            }
            if (rest_reminder_waiting_time_value * 60 * 60 > 60 * 60 * 24)
            {
                MessageBox.Show("「休憩リマインダー｜通知を出すまでの待機時間」の入力値が設定できる最大値（1440分）を超えています。", "エラー"); // 要ローカライズ
                return true;
            }
            // 秒単位に直して保存する
            ((Dictionary<string, object>)settings["data"])["rest_reminder_waiting_time"] = rest_reminder_waiting_time_value * 60 * 60;
            ((Dictionary<string, object>)settings["data"])["initial_setting_reminder_feature"] = initial_setting_reminder_feature.IsChecked ?? false;
            bool restart_required = false;
            if (((KeyValuePair<string, string>)language_combo_box.SelectedItem).Key != (string)((Dictionary<string, object>)settings["data"])["language"])
            {
                if (MessageBox.Show("言語を変更するとアプリケーションが再起動します。本当に変更しますか？", "言語の変更", MessageBoxButton.YesNo) == MessageBoxResult.No) // 要ローカライズ
                {
                    return true;
                }
                // 言語の変更を反映するために再起動を予約する
                restart_required = true;
            }
            ((Dictionary<string, object>)settings["data"])["language"] = ((KeyValuePair<string, string>)language_combo_box.SelectedItem).Key;
            Settings.settings = settings;
            if (restart_required == true)
            {
                // 予約されている場合に再起動する
                Functions.RestartApplication();
            }
            // falseは「正常に完了」、trueは「キャンセルされた」
            return false;
        }

        /// <summary>
        /// サービス設定のアクションボタン1がクリックされた際の処理
        /// </summary>
        private async void Service_action_button1_Click(object sender, RoutedEventArgs e)
        {
            if ((int)((Button)sender).Tag == 0 || (int)((Button)sender).Tag == 1)
            {
                // アカウントログインウィンドウを表示する
                bool relogin = (int)((Button)sender).Tag == 1;
                if (new AccountLoginWindow(relogin).ShowDialog() == true)
                {
                    // サービスの認証情報が変更された場合にサービス設定を読み込み直す
                    await Load_service_settings();
                }
            }
        }

        /// <summary>
        /// サービス設定のアクションボタン2がクリックされた際の処理
        /// </summary>
        private async void Service_action_button2_Click(object sender, RoutedEventArgs e)
        {
            if ((int)((Button)sender).Tag == 0)
            {
                if (MessageBox.Show("全てのサービス設定が消去されます。本当に実行しますか？", "サービス設定の消去", MessageBoxButton.YesNo) == MessageBoxResult.No) // 要ローカライズ
                {
                    return;
                }
                // サービスの認証トークンを更新する（更新前のトークンは無効化される）
                await Functions.RefreshAPITokens();
                // サービス設定を消去する
                Dictionary<string, object> settings = Settings.settings;
                ((Dictionary<string, object>)settings["data"])["connected_service"] = new Dictionary<string, object>((Dictionary<string, object>)((Dictionary<string, object>)Settings.initial_setting_data["data"])["connected_service"]);
                Settings.settings = settings;
                // サービス設定を読み込み直す
                await Load_service_settings();
            }
        }

        /// <summary>
        /// 設定初期化ボタンがクリックされた際の処理
        /// </summary>
        private async void Setting_initializing_button_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("全ての設定が初期化されます。本当に実行しますか？\n\n※以下の設定項目は初期化後に現在の設定値に復元されます。\n・言語", "設定の初期化", MessageBoxButton.YesNo) == MessageBoxResult.No) // 要ローカライズ
            {
                return;
            }
            // サービスの認証トークンを更新する（更新前のトークンは無効化される）
            if (service_action_button1.Tag == null)
            {
                await Functions.RefreshAPITokens();
            }
            else if ((int)service_action_button1.Tag == 1)
            {
                await Functions.RefreshAPITokens();
            }
            // アプリケーション設定を初期化する
            string current_language_setting = (string)((Dictionary<string, object>)Settings.settings["data"])["language"];
            Settings.InitializeSettings();
            // 初期化する前の言語の設定を復元する
            Dictionary<string, object> settings = Settings.settings;
            ((Dictionary<string, object>)settings["data"])["language"] = current_language_setting;
            Settings.settings = settings;
            // アプリケーション設定を読み込む
            Load_settings();
            await Load_service_settings();
        }
    }
}
