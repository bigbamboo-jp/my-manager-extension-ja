using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace My_Manager_Extension
{
    /// <summary>
    /// メインウィンドウ
    /// 機能：アプリケーションの基盤
    /// （MainWindow.xaml の相互作用ロジック）
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            mainwindow = this;
            application_launch_datetime = DateTime.Now;
        }

        // ウィンドウのインスタンス
        public static Window mainwindow = null;

        // アプリケーションを起動した日時
        private DateTime? application_launch_datetime = null;

        // ウィンドウ表示状態のロック
        private bool window_display_state_lock = false;

        // タスクトレイアイコン
        private System.Windows.Forms.NotifyIcon notify_icon = null;

        // バックグラウンド処理を呼び出すためのタイマー
        DispatcherTimer background_processing_timer1 = new DispatcherTimer();
        DispatcherTimer background_processing_timer2 = new DispatcherTimer();
        DispatcherTimer background_processing_timer3 = new DispatcherTimer();

        // バックグラウンド処理でサービスへの接続に失敗した回数
        private int connection_failure_count = 0;

        /// <summary>
        /// ウィンドウが表示された（＝アプリケーションが起動した）際の処理
        /// </summary>
        private async void Window_Initialized(object sender, EventArgs e)
        {
            // 引数に設定の初期化を行うオプションが含まれている場合
            if (Environment.GetCommandLineArgs().Contains("--reset-settings") == true || Environment.GetCommandLineArgs().Contains("-r") == true)
            {
                if (MessageBox.Show("全ての設定が初期化されます。本当に実行しますか？", "設定の初期化" + " - " + Settings.assembly_name, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) // 要ローカライズ
                {
                    // 設定を初期化する（設定ファイルの削除のみ行う）
                    Settings.InitializeSettings();
                }
                // アプリケーションを終了する
                Environment.Exit(0);
                return;
            }
            // トースト通知に対する操作を購読する
            Functions.StartListeningToNotificationActivation();
            // タスクトレイアイコンを表示する
            Change_notify_icon_state(action: 0);
            // サービス名ラベルのコンテンツを設定する（アプリケーション名は仮）
            service_name_label.Content = Settings.assembly_name;
            // ユーザーのステータスを取得して表示する
            await Update_status_display();
            // バックグラウンド処理の管理メソッドを呼び出す
            await Manage_background_processing();
        }

        /// <summary>
        /// バックグラウンド処理の管理をするメソッド
        /// </summary>
        public async Task Manage_background_processing(bool reset_mode = false)
        {
            // リセットモードの場合はタイマー・カウントを初期化する
            if (reset_mode == true)
            {
                background_processing_timer1.Stop();
                background_processing_timer1 = new DispatcherTimer();
                background_processing_timer2.Stop();
                background_processing_timer2 = new DispatcherTimer();
                background_processing_timer3.Stop();
                background_processing_timer3 = new DispatcherTimer();
                connection_failure_count = 0;
            }
            // APIの認証状況を確認して、それに応じて処理をする
            Dictionary<string, object> settings = (Dictionary<string, object>)Settings.settings["data"];
            int api_authorization_state = await Functions.ConfirmAPIAuthorizationState();
            switch (api_authorization_state)
            {
                case 0:
                    // 状況：トークンが有効であり、データにアクセス可能である
                    // 処理1：まだ出席を記録していない場合に出席記録のリマインダーをスケジュールする
                    Dictionary<string, object> todays_entry_status = await Functions.GetTodaysEntryStatus();
                    if ((int)todays_entry_status["status"] == 1)
                    {
                        background_processing_timer1 = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60 * 1) };
                        background_processing_timer1.Tick += async (sender, e) =>
                        {
                            // 一定時間が経過した後でも出席を記録していなければ、リマインダーを表示する
                            if (DateTime.Now >= (application_launch_datetime + TimeSpan.FromSeconds((int)settings["entry_reminder_waiting_time"])))
                            {
                                Dictionary<string, object> todays_entry_status = await Functions.GetTodaysEntryStatus();
                                if (todays_entry_status != null)
                                {
                                    // スケジュールをキャンセルした後、管理メソッドを呼び出す
                                    ((DispatcherTimer)sender).Stop();
                                    await Manage_background_processing();
                                }
                                else
                                {
                                    if ((int)todays_entry_status["status"] == 1)
                                    {
                                        // 出席記録のリマインダーを表示する
                                        if ((bool)settings["entry_reminder_feature"] == true)
                                        {
                                            Functions.SendToastNotification(situation: 1);
                                        }
                                    }
                                }
                            }
                        };
                        background_processing_timer1.Start();
                    }
                    // 処理2：作業中の状況の場合に連続作業時間のリマインダーをスケジュールする
                    DateTime? last_notification_sent_attendance_record_entry_time = null;
                    DispatcherTimer background_processing_timer2 = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60 * 1) };
                    background_processing_timer2.Tick += async (sender, e) =>
                    {
                        // 現在の出席記録で一度もリマインダーを表示していないことを確認する
                        Dictionary<string, object> todays_entry_status = await Functions.GetTodaysEntryStatus();
                        if (todays_entry_status == null)
                        {
                            // スケジュールをキャンセルした後、管理メソッドを呼び出す
                            ((DispatcherTimer)sender).Stop();
                            await Manage_background_processing();
                        }
                        else
                        {
                            if (todays_entry_status["last_entry_time"] != null && (DateTime)todays_entry_status["last_entry_time"] != last_notification_sent_attendance_record_entry_time)
                            {
                                // 連続作業時間がユーザーが設定した値よりも大きい場合にリマインダーを表示する
                                TimeSpan continuous_working_time = DateTime.Now - (DateTime)todays_entry_status["last_entry_time"];
                                if (continuous_working_time >= TimeSpan.FromSeconds((int)settings["rest_reminder_waiting_time"]))
                                {
                                    // 現在の出席記録でリマインダーを表示したことを記録する
                                    last_notification_sent_attendance_record_entry_time = (DateTime)todays_entry_status["last_entry_time"];
                                    // 休憩リマインダーを表示する
                                    if ((bool)settings["rest_reminder_feature"] == true)
                                    {
                                        Functions.SendToastNotification(situation: 2);
                                    }
                                }
                            }
                        }
                    };
                    background_processing_timer2.Start();
                    connection_failure_count = 0;
                    break;
                case 3:
                    // 状況：トークンが無効であり、アクセス不可能である
                    // 処理：再ログインを促すリマインダーを表示する
                    Functions.SendToastNotification(situation: 3);
                    connection_failure_count = 0;
                    break;
                case 10:
                    // 状況：サービスにログインしていないため、アクセス不可能である
                    // 処理：ログインを促すリマインダーを表示する
                    if ((bool)settings["initial_setting_reminder_feature"] == true)
                    {
                        Functions.SendToastNotification(situation: 10);
                    }
                    connection_failure_count = 0;
                    break;
                case 11:
                    // 状況：認証は済んでいるがサーバーに接続できないためトークンを検証できない
                    // 処理：数分後に再確認する
                    background_processing_timer3 = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60 * 1) };
                    background_processing_timer3.Tick += async (sender, e) =>
                    {
                        ((DispatcherTimer)sender).Stop();
                        await Manage_background_processing();
                    };
                    background_processing_timer3.Start();
                    connection_failure_count += 1;
                    if (connection_failure_count == 10) // 10回目のみ
                    {
                        // 10回連続でサーバーに接続できなかった場合に通知を表示する
                        Functions.SendToastNotification(situation: 11);
                    }
                    break;
                case 12:
                    // 状況：設定データが壊れているため、アクセス不可能である
                    // 処理：再ログインを促すリマインダーを表示する
                    Functions.SendToastNotification(situation: 12);
                    connection_failure_count = 0;
                    break;
            }
        }

        /// <summary>
        /// ウィンドウが非アクティブになった際の処理
        /// </summary>
        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                // ウィンドウが表示されている場合にウィンドウを非表示にする
                Change_window_display_state(null, null);
            }
        }

        /// <summary>
        /// ウィンドウが閉じられた（＝アプリケーションが終了する）際の処理
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            // httpクライアントを破棄する
            if (Functions.http_client != null)
            {
                Functions.http_client.Dispose();
            }
            // タスクトレイアイコンを非表示にする
            Change_notify_icon_state(action: 1);
            // トースト通知に対する操作の購読をやめる
            Functions.StopListeningToNotificationActivation();
        }

        /// <summary>
        /// タスクトレイアイコンの表示状態を変更するメソッド
        /// </summary>
        private void Change_notify_icon_state(int action)
        {
            // タスクトレイアイコンが既に表示されている場合は一度非表示にする
            if (notify_icon != null)
            {
                notify_icon.Dispose();
                notify_icon = null;
            }
            // タスクトレイアイコンの表示が要求された場合
            if (action == 0)
            {
                // タスクトレイアイコン本体を生成する
                System.Windows.Forms.ContextMenuStrip context_menu_strip = new System.Windows.Forms.ContextMenuStrip();
                using Stream icon_stream = Application.GetResourceStream(new Uri("pack://application:,,,/Icons/icon2.ico", UriKind.Absolute)).Stream;
                notify_icon = new System.Windows.Forms.NotifyIcon
                {
                    Text = Settings.assembly_name,
                    Icon = new System.Drawing.Icon(icon_stream),
                    Visible = true,
                    ContextMenuStrip = context_menu_strip
                };
                notify_icon.MouseClick += new System.Windows.Forms.MouseEventHandler(Change_window_display_state);
                // ウィンドウを表示するメニューアイテムを生成する
                System.Windows.Forms.ToolStripMenuItem show_menu_item = new System.Windows.Forms.ToolStripMenuItem
                {
                    Text = "メニューを表示する" // 要ローカライズ
                };
                // メニューアイテムが描画される際の処理
                void Show_menu_item_Paint(object? sender, System.Windows.Forms.PaintEventArgs e)
                {
                    if (this.Visibility == Visibility.Visible)
                    {
                        // ウィンドウが表示されている場合
                        ((System.Windows.Forms.ToolStripMenuItem)sender).Text = "メニューを閉じる"; // 要ローカライズ
                    }
                    else
                    {
                        // ウィンドウが表示されていない場合
                        ((System.Windows.Forms.ToolStripMenuItem)sender).Text = "メニューを表示する"; // 要ローカライズ
                    }
                }
                show_menu_item.Paint += new System.Windows.Forms.PaintEventHandler(Show_menu_item_Paint);
                show_menu_item.MouseDown += new System.Windows.Forms.MouseEventHandler(Change_window_display_state);
                context_menu_strip.Items.Add(show_menu_item);
                // メニューアイテムの区切り線（1つ目）を生成する
                System.Windows.Forms.ToolStripSeparator separator1 = new System.Windows.Forms.ToolStripSeparator();
                context_menu_strip.Items.Add(separator1);
                // アプリケーションを終了するメニューアイテムを生成する
                System.Windows.Forms.ToolStripMenuItem exit_program_item = new System.Windows.Forms.ToolStripMenuItem
                {
                    Text = "プログラムを終了する" // 要ローカライズ
                };
                // メニューアイテムがクリックされた際の処理
                void Exit_program_item_Click(object? sender, System.Windows.Forms.MouseEventArgs e)
                {
                    // アプリケーションを終了する
                    Application.Current.Shutdown();
                }
                exit_program_item.MouseDown += new System.Windows.Forms.MouseEventHandler(Exit_program_item_Click);
                context_menu_strip.Items.Add(exit_program_item);
            }
        }

        /// <summary>
        /// ウィンドウの表示状態を変更するメソッド
        /// </summary>
        private async void Change_window_display_state(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e != null)
            {
                if (e.Button != System.Windows.Forms.MouseButtons.Left)
                {
                    // 左ボタン以外が押された場合は処理をキャンセルする
                    return;
                }
            }
            if (window_display_state_lock == true)
            {
                // ウィンドウ表示状態のロックが有効な場合は処理をキャンセルする
                return;
            }
            // 一定時間、ウィンドウの表示状態をロックする
            window_display_state_lock = true;
            System.Timers.Timer timer1 = new System.Timers.Timer(100);
            timer1.Elapsed += (s, e) => { window_display_state_lock = false; };
            timer1.AutoReset = false;
            timer1.Start();
            if (this.Visibility == Visibility.Visible)
            {
                // ウィンドウを非表示にする
                this.Visibility = Visibility.Hidden;
            }
            else
            {
                // ウィンドウをマウスポインターの近くに表示する
                double margin = 25.0;
                System.Drawing.Point cursor_position = System.Windows.Forms.Cursor.Position;
                this.Left = cursor_position.X * (SystemParameters.PrimaryScreenWidth / System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width) - (this.Width + margin);
                this.Top = cursor_position.Y * (SystemParameters.PrimaryScreenHeight / System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height) - (this.Height + margin);
                this.Visibility = Visibility.Visible;
                this.Activate();
                // ステータス表示を更新する
                await Update_status_display();
            }
        }

        /// <summary>
        /// ユーザーのサービスでのステータスを取得して表示するメソッド
        /// </summary>
        private async Task Update_status_display()
        {
            // 現在ユーザーのステータスが表示されていない場合のみ読み込み中の表示を出す
            if ((string)service_name_label.Content == Settings.assembly_name)
            {
                user_full_name_label.Content = string.Empty;
                user_status_label.Content = "読み込み中..."; // 要ローカライズ
                action_button1.Visibility = Visibility.Hidden;
                action_button2.Visibility = Visibility.Hidden;
            }
            // APIの認証状況を確認して、それに応じて処理をする
            int api_authorization_state = await Functions.ConfirmAPIAuthorizationState();
            if (api_authorization_state == 10)
            {
                // サービスにログインしていない場合
                service_name_label.Content = Settings.assembly_name;
                user_full_name_label.Content = string.Empty;
                user_status_label.Content = "ログインしていません。"; // 要ローカライズ
                action_button1.Visibility = Visibility.Visible;
                action_button1.Tag = 3;
                action_button1.Content = "ログインする"; // 要ローカライズ
                action_button2.Visibility = Visibility.Hidden;
                action_button2.Tag = -1;
                action_button2.Content = string.Empty;
            }
            else
            {
                // サービスにログインしている場合
                try
                {
                    string service_name = await Functions.GetServiceName();
                    if (service_name == null)
                    {
                        // サービス名が取得できなかった場合は例外を発生させる
                        throw new Exception();
                    }
                    Dictionary<string, object> user_profile = await Functions.GetUserProfile();
                    string user_full_name_template = ((string)user_profile["full_name_template"]).Replace("${first_name}", "{0}").Replace("${last_name}", "{1}");
                    string user_full_name = string.Format(user_full_name_template, (string)user_profile["first_name"], (string)user_profile["last_name"]).Trim();
                    Dictionary<string, object> todays_entry_status = await Functions.GetTodaysEntryStatus();
                    service_name_label.Content = service_name;
                    user_full_name_label.Content = user_full_name;
                    action_button1.Visibility = Visibility.Visible;
                    // ユーザーの出席が記録されているかに応じて処理をする
                    switch ((int)todays_entry_status["status"])
                    {
                        case 1:
                            // 出席が記録されていない場合
                            user_status_label.Content = "出席状況: 今日は出席していません"; // 要ローカライズ
                            action_button1.Tag = 1;
                            action_button1.Content = "出席を記録する"; // 要ローカライズ
                            break;
                        case 2:
                            // 退席が記録されている場合
                            user_status_label.Content = "出席状況: 退席しています"; // 要ローカライズ
                            action_button1.Tag = 1;
                            action_button1.Content = "出席を記録する"; // 要ローカライズ
                            break;
                        case -1:
                            // 出席が記録されているが、退席が記録されていない場合（1回目）
                            user_status_label.Content = "出席状況: 出席済み(1回目)、作業中" + Environment.NewLine + "出席時刻: " + ((DateTime)todays_entry_status["last_entry_time"]).ToString("H時m分"); // 要ローカライズ
                            action_button1.Tag = 2;
                            action_button1.Content = "退席を記録する"; // 要ローカライズ
                            break;
                        case -2:
                            // 出席が記録されているが、退席が記録されていない場合（2回目～）
                            user_status_label.Content = "出席状況: 出席済み(" + ((int)todays_entry_status["attendance_count"]).ToString() + "回目)、作業中" + Environment.NewLine + "出席時刻: " + ((DateTime)todays_entry_status["last_entry_time"]).ToString("H時m分"); // 要ローカライズ
                            action_button1.Tag = 2;
                            action_button1.Content = "退席を記録する"; // 要ローカライズ
                            break;
                    }
                    action_button2.Visibility = Visibility.Visible;
                    action_button2.Tag = 1;
                    action_button2.Content = "メインメニューを表示する"; // 要ローカライズ
                }
                catch (Exception)
                {
                    // 通信エラーが発生した場合
                    service_name_label.Content = Settings.assembly_name;
                    user_full_name_label.Content = "(取得できません)"; // 要ローカライズ
                    user_status_label.Content = "出席状況: (取得できません)"; // 要ローカライズ
                    action_button1.Visibility = Visibility.Visible;
                    action_button1.Tag = 4;
                    action_button1.Content = "再読み込みする"; // 要ローカライズ
                    action_button2.Visibility = Visibility.Visible;
                    action_button2.Tag = 1;
                    action_button2.Content = "メインメニューを表示する"; // 要ローカライズ
                }
            }
        }

        /// <summary>
        /// 設定アイコンがクリックされた際の処理
        /// </summary>
        private void Setting_button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // 設定ウィンドウを表示する
            Functions.SettingsWindow_Show();
        }

        /// <summary>
        /// アクションボタン1がクリックされた際の処理
        /// </summary>
        private async void Action_button1_Click(object sender, RoutedEventArgs e)
        {
            switch ((int)((Button)sender).Tag)
            {
                case 1:
                    // サービスの出席記録ページを開く
                    Functions.OpenServicePage(target_number: 1);
                    break;
                case 2:
                    // サービスの退席記録ページを開く
                    Functions.OpenServicePage(target_number: 2);
                    break;
                case 3:
                    // サービスにログインするためのウィンドウを表示する
                    new AccountLoginWindow(relogin: false).ShowDialog();
                    break;
                case 4:
                    // ステータス表示を更新する
                    ((Button)sender).IsEnabled = false;
                    await Update_status_display();
                    ((Button)sender).IsEnabled = true;
                    break;
            }
        }

        /// <summary>
        /// アクションボタン2がクリックされた際の処理
        /// </summary>
        private void Action_button2_Click(object sender, RoutedEventArgs e)
        {
            switch ((int)((Button)sender).Tag)
            {
                case 1:
                    // サービスのメインメニューのページを開く
                    Functions.OpenServicePage(target_number: 0);
                    break;
            }
        }
    }
}
