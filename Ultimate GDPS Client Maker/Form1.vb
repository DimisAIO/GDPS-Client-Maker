Imports System.IO
Imports System.IO.Compression
Imports System.Net
Public Class Form1
    ' Top 10 Spaghetti Code
    Private Sub PrintLog(Log As String)
        TextBox2.Text += Log + Environment.NewLine
    End Sub
    Private Function Platform(FPath As String) As Integer
        ' 1 => Windows
        ' 2 => iOS
        ' 3 => Android
        If FPath.EndsWith(".exe") Then
            Return 1
        ElseIf FPath.EndsWith(".ipa") Then
            Return 2
        ElseIf FPath.EndsWith(".apk") Then
            Return 3
        Else Return 0
        End If
    End Function
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then
            TextBox1.Text = OpenFileDialog1.FileName
            Select Case Platform(OpenFileDialog1.FileName)
                Case 1
                    PrintLog("Detected: Windows")
                Case 2
                    PrintLog("Detected: iOS")
                Case 3
                    PrintLog("Detected: Android")
                Case Else
                    PrintLog("Detected: No OS")
            End Select
        End If
    End Sub

    Private Sub ReplaceBinary(BinaryPath As String, PSUrl As String, BundleName As String, PSName As String, Optional NewBinaryPC As String = "")
        Dim BinaryBytes As Byte() = File.ReadAllBytes(BinaryPath)
        Dim Binary As String = System.Text.Encoding.Default.GetString(BinaryBytes)
        ' Basic Replacing
        Binary = Binary.Replace("http://www.boomlings.com/database", PSUrl)
        Binary = Binary.Replace("https://www.boomlings.com/database", PSUrl + "/")
        Binary = Binary.Replace(Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("http://www.boomlings.com/database")), Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(PSUrl)))
        If Not String.IsNullOrEmpty(BundleName) Then
            Dim BundleOriginalName = "com.robtopx.geometryjump"
            If BundleName.Length = 23 Then
                ' iOS
                BundleOriginalName = "com.robtop.geometryjump"
                If BinaryPath.EndsWith(".plist") Then
                    Binary = Binary.Replace("Geometry", $"{PSName}")
                    ' I am Lazy :)
                    Binary = Binary.Replace($"{PSName}Jump", "GeometryJump")
                End If
            End If
            Binary = Binary.Replace(BundleOriginalName, BundleName)
        End If
        BinaryBytes = System.Text.Encoding.Default.GetBytes(Binary)
        If String.IsNullOrEmpty(NewBinaryPC) Then
            File.WriteAllBytes(BinaryPath, BinaryBytes)
        Else
            File.WriteAllBytes(NewBinaryPC, BinaryBytes)
        End If
    End Sub

    Private Function StartAndWait(AppPath As String, Arguments As String)
        Dim P As New ProcessStartInfo
        P.FileName = AppPath
        P.Arguments = Arguments
        P.UseShellExecute = False
        P.RedirectStandardError = True
        P.CreateNoWindow = True
        Dim Hi = Process.Start(P)
        If Hi IsNot Nothing Then
            Hi.WaitForExit()
        Else
            MsgBox("Failed to start process: " & AppPath)
        End If
        Return Hi.ExitCode
        ' no more sigma MsgBox(Hi.StandardError.ReadLine())
    End Function
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        PrintLog("Asking for GDPS name")
        Dim TargetName = InputBox("Enter your GDPS name:")

        If String.IsNullOrEmpty(TargetName) Then
            Return
        End If

        If TextBox3.Text.Length > 33 Then
            PrintLog("URL length is too long! Please use a shorter one!")
            Return
        End If

        While TextBox3.Text.Length <> 33
            TextBox3.Text += "/"
        End While

        PrintLog("Replacing...")

        Dim BundleName = ""

        If Platform(TextBox1.Text) > 1 Then
            Dim MaxBundleLength As Double = 23
            If Platform(TextBox1.Text) = 3 Then
                PrintLog("Android detected, incrementing bundle ID length to 24")
                MaxBundleLength += 1
            Else
                PrintLog("iOS detected, keeping bundle ID length")
            End If
            BundleName = InputBox("Enter Bundle ID (com.example.mygdps). Must be " & MaxBundleLength.ToString & " characters long!")
            If String.IsNullOrEmpty(BundleName) Then
                Return
            End If
            If BundleName.Length > MaxBundleLength Then
                PrintLog("Length too long! Please make it shorter.")
                Return
            End If
            While BundleName.Length <> MaxBundleLength
                BundleName += "0"
            End While
        End If

        If Platform(TextBox1.Text) = 2 Then
            Dim DindeIOS = Path.Combine(Environment.CurrentDirectory, "dindetemp_ios")
            PrintLog("Extracting IPA")
            ZipFile.ExtractToDirectory(TextBox1.Text, DindeIOS)
            Dim BinaryPath = Path.Combine(DindeIOS, "Payload", "GeometryJump.app", "GeometryJump")
            Dim PlistPath = Path.Combine(DindeIOS, "Payload", "GeometryJump.app", "Info.plist")
            ReplaceBinary(BinaryPath, TextBox3.Text, BundleName, TargetName)
            ReplaceBinary(PlistPath, TextBox3.Text, BundleName, TargetName)
            PrintLog("Exporting IPA")
            ZipFile.CreateFromDirectory(DindeIOS, $"{TargetName}.ipa", CompressionLevel.NoCompression, False)
            Directory.Delete(DindeIOS, True)
        ElseIf Platform(TextBox1.Text) = 1 Then
            Dim TargetBinaryPath = Path.Combine(Directory.GetParent(TextBox1.Text).ToString, TargetName & ".exe")
            ReplaceBinary(TextBox1.Text, TextBox3.Text, "", TargetName, TargetBinaryPath)
        ElseIf Platform(TextBox1.Text) = 3 Then
            Dim JavaCheck = StartAndWait("where", "java")
            If JavaCheck <> 0 Then
                PrintLog("Java is not detected! If it is, go to Environment Variables on windows search and edit the Path variable to include it!")
                Return
            End If
            Dim ApkTool = Path.Combine(Environment.CurrentDirectory, "apktool")
            PrintLog("Extracting APK")
            If Not Directory.Exists(ApkTool) Then
                PrintLog("APKTool not present, downloading...")
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls12

                Dim remoteUri As String = "https://files.141412.xyz/r/apktool.zip"
                Dim fileName As String = "apktool.zip"

                Using client As New WebClient()
                    client.DownloadFile(remoteUri, fileName)
                End Using

                ZipFile.ExtractToDirectory(Path.Combine(Environment.CurrentDirectory, "apktool.zip"), Environment.CurrentDirectory)
                File.Delete(Path.Combine(Environment.CurrentDirectory, "apktool.zip"))
            End If
            StartAndWait("java", $"-jar apktool\apktool.jar d ""{TextBox1.Text}"" -o apkedit")
            ' Inshallah
            Dim File1 = Path.Combine(Environment.CurrentDirectory, "apkedit", "res", "values", "strings.xml")
            Dim File2 = Path.Combine(Environment.CurrentDirectory, "apkedit", "apktool.yml")
            Dim Path1 = Directory.GetDirectories(Path.Combine(Environment.CurrentDirectory, "apkedit", "lib"))
            For Each Dir As String In Path1
                If File.Exists(Path.Combine(Dir, "libcocos2dcpp.so")) Then
                    ReplaceBinary(Path.Combine(Dir, "libcocos2dcpp.so"), TextBox3.Text, BundleName, TargetName)
                ElseIf File.Exists(Path.Combine(Dir, "libgame.so")) Then
                    ReplaceBinary(Path.Combine(Dir, "libgame.so"), TextBox3.Text, BundleName, TargetName)
                End If
            Next
            File.WriteAllText(File1, File.ReadAllText(File1).Replace("Geometry Dash", TargetName))
            File.WriteAllText(File2, File.ReadAllText(File2).Replace("renameManifestPackage: null", $"renameManifestPackage: {BundleName}"))
            StartAndWait("java", $"-jar apktool\apktool.jar b apkedit -o build_temp.apk")
            StartAndWait("apktool\zipalign.exe", $"4 build_temp.apk ""{TargetName}.apk""")
            File.Delete(Path.Combine(Environment.CurrentDirectory, "build_temp.apk"))
            StartAndWait("java", $"-jar apktool\apksigner.jar sign --ks apktool\demo.jks --ks-pass pass:123456 ""{TargetName}.apk""")
            Directory.Delete(Path.Combine(Environment.CurrentDirectory, "apkedit"), True)
        Else
            PrintLog("No platform detected")
            Return
        End If

        PrintLog("GDPS Client created! Enjoy!")
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        PrintLog("Discord: https://dimisaio.be/discord")
        PrintLog("Note: Edited APKs/IPAs will be saved to the same folder as this program!")
        PrintLog("Note 2: Do not click on the program while doing its work! It will freeze and crash!")
    End Sub
End Class
