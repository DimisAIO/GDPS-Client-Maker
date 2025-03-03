Imports System.IO

Public Class Form1

    Private Sub PrintLog(Log As String)
        TextBox2.Text += Log + Environment.NewLine
    End Sub
    Private Function IsWindows(FPath As String)
        Return FPath.EndsWith(".exe")
    End Function
    Private Function IsAndroid(FPath As String)
        Return FPath.EndsWith(".so")
    End Function
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then
            TextBox1.Text = OpenFileDialog1.FileName
            Select Case True
                Case IsWindows(OpenFileDialog1.FileName)
                    PrintLog("Detected: Windows")
                Case IsAndroid(OpenFileDialog1.FileName)
                    PrintLog("Detected: Android")
                Case Else
                    PrintLog("Detected: iOS")
            End Select
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        If TextBox3.Text.Length > 33 Then
            PrintLog("URL length is too long! Please use a shorter one!")
        End If

        While TextBox3.Text.Length <> 33
            TextBox3.Text += "/"
        End While

        PrintLog("Replacing...")

        Dim BinaryBytes As Byte() = File.ReadAllBytes(TextBox1.Text)
        Dim Binary As String = System.Text.Encoding.Default.GetString(BinaryBytes)
        ' Basic Replacing
        Binary = Binary.Replace("http://www.boomlings.com/database", TextBox3.Text)
        Binary = Binary.Replace("https://www.boomlings.com/database", TextBox3.Text + "/")
        Binary = Binary.Replace(Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("http://www.boomlings.com/database")), Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(TextBox3.Text)))

        If Not IsWindows(TextBox1.Text) Then
            Dim MaxBundleLength As Integer = 23
            Dim OriginalBundle = "com.robtop.geometryjump"
            If IsAndroid(TextBox1.Text) Then
                PrintLog("Android detected, changing bundle ID to Android's")
                MaxBundleLength += 1
                OriginalBundle = "com.robtopx.geometryjump"
            Else
                PrintLog("iOS detected, not changing original bundle ID")
            End If
            Dim BundleName = InputBox("Enter Bundle ID (com.example.mygdps). Must be " + MaxBundleLength + " characters long!")
            If BundleName.Length > MaxBundleLength Then
                PrintLog("Length too long! Please make it shorter.")
                Return
            End If
            While BundleName.Length <> MaxBundleLength
                BundleName += "0"
            End While
            Binary = Binary.Replace(OriginalBundle, BundleName)
        End If

        BinaryBytes = System.Text.Encoding.Default.GetBytes(Binary)

        If IsWindows(TextBox1.Text) Then
            PrintLog("Windows detected: Asking for GDPS name")
            Dim TargetName = InputBox("Enter your GDPS name:")
            File.WriteAllBytes(Path.Combine(Directory.GetParent(TextBox1.Text).ToString, TargetName & ".exe"), BinaryBytes)
        Else
            File.WriteAllBytes(TextBox1.Text, BinaryBytes)
        End If
        PrintLog("GDPS Client created! Enjoy!")
    End Sub
End Class
