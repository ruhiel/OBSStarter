using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Windows.Forms;
class Program
{
    const string OBS_PROCESS_NAME = "obs64";
    const string OBS_PATH = @"C:\Program Files\obs-studio\bin\64bit\obs64.exe";
    const string SCENE_NAME = "ワイルズ";

    static void Main()
    {


        var target = "MonsterHunterWilds.exe";
        Console.WriteLine($"{target} の起動を待機中...");

        // クエリ設定
        var query = $"SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = '{target}'";

        using (var watcher = new ManagementEventWatcher(query))
        {
            // イベントが発生するまでここで待機（ブロック）する
            var e = watcher.WaitForNextEvent();

            Console.WriteLine("【検知】モンハンが起動しました。");

            // ここに実行したい処理を書く
            DoObs();


            Console.WriteLine("処理を完了して終了します。");
            // ループがないので、このままMainメソッドが終わり、アプリが終了します
        }
    }

    private static void DoObs()
    {
        // 2. OBSの起動確認と実行
        var obsProcess = Process.GetProcessesByName(OBS_PROCESS_NAME).FirstOrDefault();

        if (obsProcess == null)
        {
            Console.WriteLine("[2/3] OBSが未起動のため、起動します...");
            if (!File.Exists(OBS_PATH))
            {
                Console.WriteLine($"【エラー】OBSのパスが見つかりません: {OBS_PATH}");
                return;
            }

            // OBSを起動（作業ディレクトリを指定しないとエラーになる場合があります）
            var startInfo = new ProcessStartInfo(OBS_PATH)
            {
                WorkingDirectory = Path.GetDirectoryName(OBS_PATH)
            };
            obsProcess = Process.Start(startInfo);
        }
        else
        {
            Console.WriteLine("[2/3] OBSは既に起動しています。");
        }

        // 3. OBSが「完全に」起動する（ウィンドウが出る）まで待機
        Console.WriteLine("[3/3] OBSの初期化完了を待っています...");
        WaitForWindow(obsProcess);
        
        // --- 追加: 録画確認ダイアログ ---
        var result = MessageBox.Show(
            "モンスターハンターが起動しました。録画を開始しますか？",
            "録画確認",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1,
            MessageBoxOptions.DefaultDesktopOnly); // 最前面に出すためのオプション

        if (result == DialogResult.Yes)
        {
            Console.WriteLine("録画コマンドを送信します...");
            RunObsCmd($"scene switch \"{SCENE_NAME}\"");
            RunObsCmd("recording start");
            Console.WriteLine("【完了】録画を開始しました。");
        }
        else
        {
            Console.WriteLine("【スキップ】録画は開始しませんでした。");
        }

        Console.WriteLine("【完了】全プロセスが準備OKです。後続の処理を開始します。");
    }

    // obs-cmd を実行するヘルパー関数
    static void RunObsCmd(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "obs-cmd", // PATHが通っていない場合はフルパスを指定
                Arguments = arguments,
                CreateNoWindow = true,      // 黒い画面を出さない
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                var output = p.StandardOutput.ReadToEnd();
                Console.WriteLine($"> obs-cmd {arguments}: {output.Trim()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"【エラー】obs-cmd の実行に失敗しました: {ex.Message}");
        }
    }

    // ウィンドウが生成されるまで待機するメソッド
    static void WaitForWindow(Process proc)
    {
        // プロセスが終了せず、かつメインウィンドウのハンドルが取得できるまでループ
        while (!proc.HasExited)
        {
            proc.Refresh(); // 最新の状態に更新
            if (proc.MainWindowHandle != IntPtr.Zero)
            {
                // ウィンドウハンドルが見つかれば「起動完了」とみなす
                break;
            }
            Thread.Sleep(500); // 0.5秒おきに確認
        }
    }
}