using Spectre.Console;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace AppCopy
{
    class Program
    {
        [DllImport("user32.dll")]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        const uint CF_UNICODETEXT = 13;



        static void Main(string[] args)
        {
            string inputPath = "";
            string fileType = "";
            string outputPath = "output.md";

            // 显示标题
            AnsiConsole.Write(new Rule("[green]File Copy[/]"));
            AnsiConsole.WriteLine();

            if (args.Length == 0)
            {
                // 获取目录路径
                inputPath = AnsiConsole.Prompt(
                    new TextPrompt<string>("[green]请输入目录路径:[/]")
                    .Validate(path =>
                    {
                        return Directory.Exists(path)
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]目录不存在[/]");
                    }));

                // 获取文件类型
                fileType = AnsiConsole.Prompt(
                    new TextPrompt<string>("[green]请输入文件格式 (例如: *.py):[/]")
                    .Validate(type =>
                    {
                        return type.StartsWith("*.")
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]格式必须以 *.开头[/]");
                    }));
            }
            else
            {
                inputPath = args[0];
                fileType = args[1];
            }

            try
            {
                // 获取所有匹配的文件
                var files = Directory.GetFiles(inputPath, fileType, SearchOption.AllDirectories);

                if (files.Length == 0)
                {
                    AnsiConsole.MarkupLine("[red]未找到匹配的文件[/]");
                    return;
                }

                // 显示找到的文件数量
                AnsiConsole.MarkupLine($"[green]找到 {files.Length} 个文件[/]");

                // 创建StringBuilder来构建markdown内容
                StringBuilder markdownContent = new StringBuilder();

                // 使用进度条显示处理进度
                AnsiConsole.Progress()
                    .Start(ctx =>
                    {
                        var task = ctx.AddTask("[green]处理文件[/]", maxValue: files.Length);

                        foreach (string filePath in files)
                        {
                            string relativePath = Path.GetRelativePath(inputPath, filePath);

                            AnsiConsole.MarkupLine($"[green] {filePath} [/]");

                            // 添加markdown标题
                            markdownContent.AppendLine($"### {relativePath}");
                            markdownContent.AppendLine("```");

                            try
                            {
                                string content = File.ReadAllText(filePath);
                                markdownContent.AppendLine(content);
                            }
                            catch (Exception ex)
                            {
                                markdownContent.AppendLine($"// Error reading file: {ex.Message}");
                            }

                            markdownContent.AppendLine("```");
                            markdownContent.AppendLine();

                            task.Increment(1);
                        }
                    });

                // 写入markdown文件
                File.WriteAllText(outputPath, markdownContent.ToString());

                // 剪贴板操作
                CopyToClipboard(markdownContent.ToString());

                // 显示结果
                var table = new Table();
                table.AddColumn("结果");
                table.AddColumn("路径");
                table.AddRow("[green]成功[/]", outputPath);
                AnsiConsole.Write(table);

                AnsiConsole.MarkupLine("[green]处理完成！[/]");
            }
            catch (Exception ex)
            {
                // 显示错误信息
                AnsiConsole.MarkupLine($"[red]发生错误:[/] {ex.Message}");
                AnsiConsole.WriteLine(ex.StackTrace);
            }
        }

        public static void CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (!OpenClipboard(IntPtr.Zero)) return;

            try
            {
                EmptyClipboard();
                IntPtr hGlobal = Marshal.StringToHGlobalUni(text);
                SetClipboardData(CF_UNICODETEXT, hGlobal);
            }
            finally
            {
                CloseClipboard();
            }
        }

    }
}
