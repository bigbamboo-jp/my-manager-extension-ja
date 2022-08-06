using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace My_Manager_Extension
{
    /// <summary>
    /// 設定データに関する機能を集約したクラス
    /// </summary>
    internal class Settings
    {
        // アプリケーション名
        internal static readonly string assembly_name = Assembly.GetExecutingAssembly().GetName().Name;

        // 設定ファイルのパス
        internal static string settings_file_path
        {
            get
            {
                if (Directory.Exists(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "AppData")) == true)
                {
                    // プログラムフォルダ内に「AppData」フォルダがある場合
                    return Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "AppData", "settings.json");
                }
                else
                {
                    // プログラムフォルダ内に「AppData」フォルダがない場合
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), assembly_name, "settings.json");
                }
            }
        }

        // 付加データファイルのパス（コンピューター内で共通）
        internal static string additional_data_file_path = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "Shared", "additional_data.json");

        // 設定ファイルのファイルタイプ [製品名を含む]
        internal const string SETTINGS_FILE_TYPE = "My Manager Extension Settings File (Format Version 1)";

        // 初期状態の設定データ
        internal static readonly Dictionary<string, object> initial_setting_data = new Dictionary<string, object>
        {
            {
                "file_type", SETTINGS_FILE_TYPE
            },
            {
                "data", new Dictionary<string, object>
                {
                    { "language", "ja-JP" },
                    { "entry_reminder_feature", true },
                    { "entry_reminder_waiting_time", 60 * 5 },
                    { "rest_reminder_feature", true },
                    { "rest_reminder_waiting_time", 60 * 60 * 5 },
                    { "initial_setting_reminder_feature", true },
                    { "connected_service", new Dictionary<string, object>
                        {
                            { "server_address", ""},
                            { "access_token", "" },
                            { "refresh_token", "" }
                        }
                    }
                }
            }
        };

        // 設定データのキャッシュ（クラス内アクセスのみ）
        private static Dictionary<string, object> _setting_data_cache = null;

        // 付加データのキャッシュ
        private static byte[] _additional_data_cache = null;

        // クラス外から設定データにアクセスするためのプロパティ
        internal static Dictionary<string, object> settings
        {
            set
            {
                SetSettings(value);
                // キャッシュを更新
                _setting_data_cache = new Dictionary<string, object>(value);
            }
            get
            {
                if (_setting_data_cache == null)
                {
                    // 読み込まれていない場合にキャッシュに設定データを読み込む
                    _setting_data_cache = GetSettings();
                }
                return new Dictionary<string, object>(_setting_data_cache);
            }
        }

        /// <summary>
        /// アプリケーションの設定を読み込むメソッド
        /// </summary>
        internal static Dictionary<string, object> GetSettings()
        {
            // 設定ファイルが存在しない場合は新しく作成する
            if (File.Exists(settings_file_path) == false)
            {
                SetSettings(initial_setting_data);
            }
            // 設定ファイルを読み込む
            string settings_file_data = File.ReadAllText(settings_file_path);
            object setting_data = Functions.JsonConvert_DeserializeObject(JToken.Parse(settings_file_data));
            if (setting_data.GetType().IsGenericType == true)
            {
                // デシリアライズしたデータがジェネリックタイプの場合
                if (setting_data.GetType().GetGenericTypeDefinition() != typeof(Dictionary<,>))
                {
                    // デシリアライズしたデータがディクショナリ以外の場合
                    return null;
                }
            }
            else
            {
                // デシリアライズしたデータがジェネリックタイプ以外の場合
                return null;
            }
            return (Dictionary<string, object>)setting_data;
        }

        /// <summary>
        /// アプリケーションの設定を保存するメソッド
        /// </summary>
        internal static void SetSettings(Dictionary<string, object> setting_data)
        {
            // 設定データをシリアライズして保存する
            string settings_file_data = JsonConvert.SerializeObject(setting_data, Formatting.Indented);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settings_file_path));
                File.WriteAllText(settings_file_path, settings_file_data);
            }
            catch (UnauthorizedAccessException)
            {
                // 書き込み権限がない場合
                Assembly executing_assembly = Assembly.GetExecutingAssembly();
                if (MessageBox.Show("権限がないためファイルシステムにアクセスできません。\n管理者権限でアプリケーションを再起動しますか？\n※現在のプロセスはどのような選択をしても終了します。", "エラー" + " - " + executing_assembly.GetName().Name, MessageBoxButton.YesNo) == MessageBoxResult.Yes) // 要ローカライズ
                {
                    // ユーザーが指示した場合に管理者権限で再起動する
                    Functions.RestartApplication(run_as: true);
                }
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// アプリケーションの設定を初期化するメソッド
        /// </summary>
        internal static void InitializeSettings()
        {
            // 設定ファイル（ディレクトリ）を削除する
            if (Directory.Exists(settings_file_path) == true)
            {
                DirectoryInfo di = new DirectoryInfo(settings_file_path);
                di.Delete(true);
            }
            else
            {
                File.Delete(settings_file_path);
            }
            // 設定データのキャッシュを消去する
            _setting_data_cache = null;
        }

        /// <summary>
        /// 文字列をデータ保護API（DPAPI）を使用して暗号化するメソッド
        /// </summary>
        internal static string Encrypt_string(string raw_data)
        {
            byte[] encrypted_data = Protect(Encoding.UTF8.GetBytes(raw_data));
            return Convert.ToBase64String(encrypted_data);
        }

        /// <summary>
        /// 文字列をデータ保護API（DPAPI）を使用して復号するメソッド
        /// </summary>
        internal static string Decrypt_string(string encrypted_data)
        {
            byte[] raw_data = Unprotect(Convert.FromBase64String(encrypted_data));
            if (raw_data == null)
            {
                // 復号に失敗した場合
                return null;
            }
            else
            {
                // 復号に成功した場合
                return Encoding.UTF8.GetString(raw_data);
            }
        }

        /// <summary>
        /// データを暗号化する際に付加するデータを返すメソッド
        /// </summary>
        internal static byte[] GetAdditionalData()
        {
            // 付加データファイルが存在しない場合は新しく作成する
            if (File.Exists(additional_data_file_path) == false)
            {
                byte[] data_1 = Encryption.AESThenHMAC.NewKey();
                Dictionary<string, object> additional_data_for_writing = new Dictionary<string, object>
                {
                    { "1", Convert.ToBase64String(data_1) }
                };
                // 設定データをシリアライズして保存する
                string additional_data_file_for_writing = JsonConvert.SerializeObject(additional_data_for_writing, Formatting.Indented);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(additional_data_file_path));
                    File.WriteAllText(additional_data_file_path, additional_data_file_for_writing);
                    _additional_data_cache = data_1;
                }
                catch (UnauthorizedAccessException)
                {
                    // 書き込み権限がない場合
                    Assembly executing_assembly = Assembly.GetExecutingAssembly();
                    if (MessageBox.Show("権限がないためファイルシステムにアクセスできません。\n管理者権限でアプリケーションを再起動しますか？\n※現在のプロセスはどのような選択をしても終了します。", "エラー" + " - " + executing_assembly.GetName().Name, MessageBoxButton.YesNo) == MessageBoxResult.Yes) // 要ローカライズ
                    {
                        // ユーザーが指示した場合に管理者権限で再起動する
                        Functions.RestartApplication(run_as: true);
                    }
                    Environment.Exit(0);
                }
            }
            // キャッシュが利用できない場合に付加データファイルを読み込む
            if (_additional_data_cache == null)
            {
                string additional_data_file = File.ReadAllText(additional_data_file_path);
                object additional_data = Functions.JsonConvert_DeserializeObject(JToken.Parse(additional_data_file));
                if (additional_data.GetType().IsGenericType == true)
                {
                    // デシリアライズしたデータがジェネリックタイプの場合
                    if (additional_data.GetType().GetGenericTypeDefinition() != typeof(Dictionary<,>))
                    {
                        // デシリアライズしたデータがディクショナリ以外の場合
                        return null;
                    }
                }
                else
                {
                    // デシリアライズしたデータがジェネリックタイプ以外の場合
                    return null;
                }
                _additional_data_cache = Convert.FromBase64String((string)((Dictionary<string, object>)additional_data)["1"]);
            }
            return _additional_data_cache;
        }

        /// <summary>
        /// データ保護API（DPAPI）でデータを暗号化するメソッド
        /// ソース：https://docs.microsoft.com/dotnet/api/system.security.cryptography.protecteddata.protect
        /// </summary>
        internal static byte[] Protect(byte[] data)
        {
            try
            {
                return ProtectedData.Protect(data, GetAdditionalData(), DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        /// <summary>
        /// データ保護API（DPAPI）でデータを復号するメソッド
        /// ソース：https://docs.microsoft.com/dotnet/api/system.security.cryptography.protecteddata.unprotect
        /// </summary>
        internal static byte[] Unprotect(byte[] data)
        {
            try
            {
                return ProtectedData.Unprotect(data, GetAdditionalData(), DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }
    }
}
