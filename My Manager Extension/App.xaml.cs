using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace My_Manager_Extension
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// アプリケーションが立ち上がった際の処理
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            // 言語データを読み込む
            Functions.LoadLanguageData();
            // [多重起動チェック]
            // ミューテックスを作成する
            var mutex = new System.Threading.Mutex(false, "Global\\" + Settings.assembly_name);
            // ミューテックスの所有権を要求する
            if (!mutex.WaitOne(0, false))
            {
                // 既に同じプログラムが起動している場合にアプリケーションを終了する
                MessageBox.Show("このプログラムは既に起動しています。", "エラー" + " - " + Settings.assembly_name);
                Environment.Exit(0);
            }
        }
    }
}
