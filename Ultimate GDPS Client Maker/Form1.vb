Imports System.IO
Imports System.IO.Compression
Imports System.Net
Imports Org.BouncyCastle.Asn1.X509
Imports Org.BouncyCastle.Crypto
Imports Org.BouncyCastle.Crypto.Generators
Imports Org.BouncyCastle.Math
Imports Org.BouncyCastle.Pkcs
Imports Org.BouncyCastle.Security
Imports Org.BouncyCastle.X509
Public Class Form1
    ' Top 10 Spaghetti Code
    Dim Password As String = Nothing
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

    Public Sub CreateApkKey(keystorePath As String, password As String, aliasName As String)

        Me.Password = password
        ' Generate RSA key pair
        Dim keyGen As New RsaKeyPairGenerator()
        keyGen.Init(New KeyGenerationParameters(New SecureRandom(), 2048))
        Dim keyPair = keyGen.GenerateKeyPair()

        ' Create certificate
        Dim attrs = New X509Name("CN=Android,O=AppSigner,C=EU")
        Dim certGen = New X509V3CertificateGenerator()
        Dim serial = BigInteger.ProbablePrime(120, New Random())

        certGen.SetSerialNumber(serial)
        certGen.SetIssuerDN(attrs)
        certGen.SetSubjectDN(attrs)
        certGen.SetNotBefore(DateTime.UtcNow)
        certGen.SetNotAfter(DateTime.UtcNow.AddYears(25))
        certGen.SetSignatureAlgorithm("SHA256WITHRSA")
        certGen.SetPublicKey(keyPair.Public)

        Dim cert = certGen.Generate(keyPair.Private)

        ' Store in PKCS12 keystore
        Dim store = New Pkcs12Store()
        store.SetKeyEntry(aliasName, New AsymmetricKeyEntry(keyPair.Private),
                      New X509CertificateEntry() {New X509CertificateEntry(cert)})

        Using fs As New FileStream(keystorePath, FileMode.Create, FileAccess.Write)
            store.Save(fs, password.ToCharArray(), New SecureRandom())
        End Using

    End Sub

    Private Sub ReplaceAllOccurrences(ByRef data As Byte(), find As Byte(), replace As Byte())

        If replace.Length <> find.Length Then
            Throw New Exception("String 1's length isn't equal to String 2's")
        End If

        Dim i As Integer = 0
        While i <= data.Length - find.Length

            Dim match As Boolean = True
            For j As Integer = 0 To find.Length - 1
                If data(i + j) <> find(j) Then
                    match = False
                    Exit For
                End If
            Next

            If match Then
                Array.Copy(replace, 0, data, i, replace.Length)
                i += replace.Length   ' continue after replaced block
            Else
                i += 1
            End If
        End While

    End Sub

    Private Sub ReplaceBinary(BinaryPath As String, PSUrl As String, BundleName As String, PSName As String, Optional NewBinaryPC As String = "")

        Dim data As Byte() = File.ReadAllBytes(BinaryPath)

        ' Replace raw ASCII sequences
        ReplaceAllOccurrences(data,
        System.Text.Encoding.ASCII.GetBytes("http://www.boomlings.com/database"),
        System.Text.Encoding.ASCII.GetBytes(PSUrl))

        ReplaceAllOccurrences(data,
        System.Text.Encoding.ASCII.GetBytes("https://www.boomlings.com/database"),
        System.Text.Encoding.ASCII.GetBytes(PSUrl & "/"))

        ' Replace Base64 sequences
        ReplaceAllOccurrences(data,
        System.Text.Encoding.ASCII.GetBytes(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("http://www.boomlings.com/database"))),
        System.Text.Encoding.ASCII.GetBytes(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(PSUrl))))

        ' Bundle / plist handling
        If Not String.IsNullOrEmpty(BundleName) Then

            Dim originalBundle As String = "com.robtopx.geometryjump"

            If BundleName.Length = 23 Then
                originalBundle = "com.robtop.geometryjump"

                If BinaryPath.EndsWith(".plist") Then
                    ReplaceAllOccurrences(data,
                    System.Text.Encoding.ASCII.GetBytes("Geometry"),
                    System.Text.Encoding.ASCII.GetBytes(PSName))

                    ReplaceAllOccurrences(data,
                    System.Text.Encoding.ASCII.GetBytes(PSName & "Jump"),
                    System.Text.Encoding.ASCII.GetBytes("GeometryJump"))
                End If
            End If

            ReplaceAllOccurrences(data,
            System.Text.Encoding.ASCII.GetBytes(originalBundle),
            System.Text.Encoding.ASCII.GetBytes(BundleName))
        End If

        ' Save
        Dim outputPath As String = If(String.IsNullOrEmpty(NewBinaryPC), BinaryPath, NewBinaryPC)
        File.WriteAllBytes(outputPath, data)

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
            If Not File.Exists(Path.Combine(Environment.CurrentDirectory, "gdps.keystore")) Then
                PrintLog("Please create a keyset from File -> Make APK Certificate!")
            End If
            Dim JavaCheck = StartAndWait("where", "java")
            If JavaCheck <> 0 Then
                PrintLog("Java is not detected! If it is, go to Environment Variables on windows search and edit the Path variable to include it!")
                Return
            End If
            Dim ApkTool = Path.Combine(Environment.CurrentDirectory, "apktool")
            PrintLog("Extracting APK")
            If Not Directory.Exists(ApkTool) Then
                PrintLog("APKTool not present, please copy the folder to the same place as the APK Maker!")
                Process.Start("https://files.141412.xyz/r/apktool.zip")
                Return
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
            If Password Is Nothing Then Password = InputBox("Enter Key Password: ")
            StartAndWait("java", $"-jar apktool\apksigner.jar sign --ks gdps.keystore --ks-pass pass:{Password} ""{TargetName}.apk""")
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

    Private Sub MakeAPKCertificateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MakeAPKCertificateToolStripMenuItem.Click
        Dim User As String = InputBox("Username: ")
        Dim Pass As String = InputBox("Password: ")
        If Not String.IsNullOrEmpty(User) AndAlso Not String.IsNullOrEmpty(Pass) Then
            CreateApkKey("gdps.keystore", "mypassword", "myalias")
            If File.Exists(Path.Combine(Environment.CurrentDirectory, "gdps.keystore")) Then
                PrintLog("Key Generated!")
            Else
                PrintLog("Failed to create key!")
            End If
        Else
            PrintLog("Invalid values detected!")
        End If
    End Sub

    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        MsgBox("ultimate gdps client maker, free open source. i hope it works lol")
    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Close()
    End Sub
End Class
