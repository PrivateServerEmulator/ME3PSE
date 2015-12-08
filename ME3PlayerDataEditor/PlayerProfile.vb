Option Strict On

Namespace PlayerProfile

    Public Class ME3MP_Profile
        ' REQUIRED
        Public Lines() As String
        Public Header As ME3PlayerHeader
        Public Base As ME3PlayerBase
        Public Classes(5) As ME3PlayerClass
        Public Chars As List(Of ME3PlayerChar)
        ' OPTIONAL
        Public FaceCodes As ME3PlayerFaceCodes
        Public NewItem As ME3PlayerNewItem
        Public Banner As ME3PlayerBanner
        Public ChallengeStats As ME3PlayerChallengeStats

        Public Shared Function InitializeFromFile(ByVal Filename As String) As ME3MP_Profile
            Dim profile As New ME3MP_Profile
            Try
                'System.Diagnostics.Debug.Print("ME3MP_Profile.InitializeFromFile / Load file")
                profile.Lines = IO.File.ReadAllLines(Filename)
                ' Header
                'System.Diagnostics.Debug.Print("ME3MP_Profile.InitializeFromFile / Header")
                profile.Header = New ME3PlayerHeader(profile.Lines)
                ' Base
                'System.Diagnostics.Debug.Print("ME3MP_Profile.InitializeFromFile / Base")
                Dim x As Integer = profile.SeekLine("Base=")
                profile.Base = New ME3PlayerBase(profile.Lines(x).Substring(5))
                ' Class
                'System.Diagnostics.Debug.Print("ME3MP_Profile.InitializeFromFile / Class")
                Dim I As Integer
                For I = 1 To 6
                    x = profile.SeekLine("class" & I & "=")
                    profile.Classes(I - 1) = New ME3PlayerClass(profile.Lines(x).Substring(7))
                Next I
                ' Chars
                'System.Diagnostics.Debug.Print("ME3MP_Profile.InitializeFromFile / Chars")
                profile.Chars = New List(Of ME3PlayerChar)
                I = 0 ' start at 0
                Do
                    Dim strchar As String = "char" & I & "="
                    x = profile.SeekLine(strchar)
                    If x = -1 Then Exit Do
                    ' add char to profile main collection
                    profile.Chars.Add(New ME3PlayerChar(profile.Lines(x).Substring(strchar.Length)))
                    ' add char to its respective class collection
                    AddCharToClass(profile, profile.Chars(I).KitClassName, I)
                    I += 1
                Loop While True
                ' start of optional stuff
                ' FaceCodes
                'System.Diagnostics.Debug.Print("ME3MP_Profile.InitializeFromFile / FaceCodes")
                x = profile.SeekLine("FaceCodes=")
                If x <> -1 Then profile.FaceCodes = New ME3PlayerFaceCodes(profile.Lines(x))
                ' NewItem
                'System.Diagnostics.Debug.Print("ME3MP_Profile.InitializeFromFile / NewItem")
                x = profile.SeekLine("NewItem=")
                If x <> -1 Then profile.NewItem = New ME3PlayerNewItem(profile.Lines(x))
                ' Banner
                'System.Diagnostics.Debug.Print("ME3MP_Profile.InitializeFromFile / Banner")
                x = profile.SeekLine("csreward=")
                If x <> -1 Then profile.Banner = New ME3PlayerBanner(profile.Lines(x))
                ' Challenge stats
                'System.Diagnostics.Debug.Print("ME3MP_Profile.InitializeFromFile / ChallengeStats")
                Dim CSparam(5) As String
                x = profile.SeekLine("Completion=")
                If x <> -1 Then CSparam(0) = profile.Lines(x).Substring(11)
                x = profile.SeekLine("Progress=")
                If x <> -1 Then CSparam(1) = profile.Lines(x).Substring(9)
                x = profile.SeekLine("cscompletion=")
                If x <> -1 Then CSparam(2) = profile.Lines(x).Substring(13)
                x = profile.SeekLine("cstimestamps=")
                If x <> -1 Then CSparam(3) = profile.Lines(x).Substring(13)
                x = profile.SeekLine("cstimestamps2=")
                If x <> -1 Then CSparam(4) = profile.Lines(x).Substring(14)
                x = profile.SeekLine("cstimestamps3=")
                If x <> -1 Then CSparam(5) = profile.Lines(x).Substring(14)
                profile.ChallengeStats = New ME3PlayerChallengeStats(CSparam(0), CSparam(1), CSparam(2), CSparam(3), CSparam(4), CSparam(5))
                'System.Diagnostics.Debug.Print("ME3MP_Profile.InitializeFromFile / Done")
                Return profile
            Catch ex As Exception
                System.Diagnostics.Debug.Print("ME3MP_Profile.InitializeFromFile / " & ex.GetType().Name & " / " & ex.Message)
                Return Nothing
            End Try
        End Function

        Private Shared Sub AddCharToClass(ByVal profile As ME3MP_Profile, ByVal ClassName As String, ByVal CharIndex As Integer)
            Select Case ClassName
                Case ME3PlayerClass.CLASS1
                    profile.Classes(0).Members.Add(CharIndex)
                Case ME3PlayerClass.CLASS2
                    profile.Classes(1).Members.Add(CharIndex)
                Case ME3PlayerClass.CLASS3
                    profile.Classes(2).Members.Add(CharIndex)
                Case ME3PlayerClass.CLASS4
                    profile.Classes(3).Members.Add(CharIndex)
                Case ME3PlayerClass.CLASS5
                    profile.Classes(4).Members.Add(CharIndex)
                Case ME3PlayerClass.CLASS6
                    profile.Classes(5).Members.Add(CharIndex)
            End Select
        End Sub

#Region "Shortcut / Helper Functions"
        Public Function GetN7Rating() As Integer
            Dim TotalLevel = 0
            For I As Integer = 0 To 5
                If IsClassActive(I) Then TotalLevel += Classes(I).GetLevel
            Next I
            Return GetTotalPromotions() * 30 + TotalLevel  ' 30 -> 20 from leveling the class, plus 10 from bonus for promoting
        End Function

        Public Function GetTotalPromotions() As Integer
            Dim TotalPromotions As Integer = 0
            For I As Integer = 0 To 5
                TotalPromotions += Classes(I).GetPromotions()
            Next I
            Return TotalPromotions
        End Function

        Public Function GetChallengePoints() As Integer
            Return ChallengeStats.ChallengePoints
        End Function

        Public Function GetPlayerName() As String
            Return Header.GetDisplayName()
        End Function

        Public Function GetPlayerID() As Long
            Return Header.GetPID()
        End Function

        Public Function GetPassword() As String
            Return Header.GetAuth2()
        End Function
#End Region

        Public Function IsClassActive(ByVal ClassNumber As Integer) As Boolean
            For Each M As Integer In Classes(ClassNumber).Members
                If Chars(M).GetDeployed() Then Return True
            Next M
            Return False
        End Function

        Public Function SeekLine(ByVal str As String) As Integer
            For I As Integer = 0 To Lines.Count - 1
                If Lines(I).StartsWith(str) Then Return I
            Next I
            Return -1
        End Function

        Public Function SaveToFile(ByVal Filename As String) As Boolean
            Try
                System.IO.File.WriteAllLines(Filename, ToLines())
                Return True
            Catch ex As Exception
                System.Diagnostics.Debug.Print("ME3MP_Profile.SaveToFile / " & ex.GetType().Name & " / " & ex.Message)
                Return False
            End Try
        End Function

        Public Function ToLines(Optional ByVal IncludeHeader As Boolean = True) As List(Of String)
            Dim listLines As New List(Of String)
            ' Header : PID UID AUTH AUTH2 DSNM
            If IncludeHeader Then listLines.AddRange(Header.GetLines())
            ' Base
            listLines.Add("Base=" & Base.ToString())
            ' Classes
            For Each c As ME3PlayerClass In Classes
                listLines.Add(c.ToString())
            Next
            ' Chars
            For I As Integer = 0 To Chars.Count - 1
                listLines.Add("char" & I & "=" & Chars(I).ToString())
            Next I
            ' FaceCodes
            If FaceCodes IsNot Nothing Then listLines.Add(FaceCodes.GetLine())
            ' NewItem
            If NewItem IsNot Nothing Then listLines.Add(NewItem.GetLine())
            ' Banner
            If Banner IsNot Nothing Then listLines.Add(Banner.GetLine())
            ' Challenge Stats
            listLines.AddRange(ChallengeStats.GetLines())
            Return listLines
        End Function

    End Class

    Public Class ME3PlayerBase
        Public Const INVENTORYITEMLASTINDEX As Integer = 670

        Private SubValues(0 To 9) As Integer
        '0: unknown
        '1: unknown
        '2: credits
        '3: unknown
        '4: unknown
        '5: credits spent
        '6: unknown
        '7: games played (finished)
        '8: seconds played
        '9: unknown
        Private Items(INVENTORYITEMLASTINDEX) As Byte ' SubValues(10)

        Public Sub New(ByVal BaseValue As String)
            Dim BaseFields() As String
            BaseFields = BaseValue.Split(Char.Parse(";"))
            If BaseFields.Count <> 11 Then Throw New ArgumentException("BaseValue string must be 11 fields.")
            Dim ExpectedCharCount As Integer = (INVENTORYITEMLASTINDEX + 1) * 2
            If BaseFields(10).Length <> ExpectedCharCount Then Throw New ArgumentException("Last field of BaseValue must be " & ExpectedCharCount & " chars (" & (INVENTORYITEMLASTINDEX + 1) & " inventory items).")
            For I As Integer = 0 To 9
                Me.SubValues(I) = Integer.Parse(BaseFields(I))
            Next I
            StripIntoItemValues(BaseFields(10))
        End Sub

        Private Sub StripIntoItemValues(ByVal BigString As String)
            For I As Integer = 0 To INVENTORYITEMLASTINDEX
                'I think I can do this shit in one line...
                Me.Items(I) = Byte.Parse(BigString.Substring(I * 2, 2), Globalization.NumberStyles.HexNumber)
            Next
        End Sub

        Public Function GetItem(ByVal Index As Integer) As Byte
            Return Me.Items(Index)
        End Function
        Public Sub SetItem(ByVal Index As Integer, ByVal Value As Byte)
            Me.Items(Index) = Value
        End Sub

        Public Function GetCredits() As Integer
            Return Me.SubValues(2)
        End Function
        Public Sub SetCredits(ByVal intCredits As Integer)
            Me.SubValues(2) = intCredits
        End Sub

        Public Function GetGamesPlayed() As Integer
            Return Me.SubValues(7)
        End Function
        Public Sub SetGamesPlayed(ByVal intGamesPlayed As Integer)
            Me.SubValues(7) = intGamesPlayed
        End Sub

        Public Function GetTimePlayedSeconds() As Integer
            Return Me.SubValues(8)
        End Function
        Public Sub SetTimePlayed(ByVal intSeconds As Integer)
            Me.SubValues(8) = intSeconds
        End Sub
        Public Sub SetTimePlayed(ByVal intMinutes As Integer, ByVal intSeconds As Integer)
            Me.SetTimePlayed(60 * intMinutes + intSeconds)
        End Sub
        Public Sub SetTimePlayed(ByVal intHours As Integer, ByVal intMinutes As Integer, ByVal intSeconds As Integer)
            Me.SetTimePlayed(3600 * intHours + 60 * intMinutes + intSeconds)
        End Sub

        Public Overrides Function ToString() As String
            Dim sb As New System.Text.StringBuilder
            For I As Integer = 0 To 9
                sb.Append(Me.SubValues(I))
                sb.Append(";")
            Next I
            sb.Append(GetItemsString())
            Return sb.ToString
        End Function

        Private Function GetItemsString() As String
            Dim sb As New System.Text.StringBuilder
            For I As Integer = 0 To INVENTORYITEMLASTINDEX
                sb.Append(Me.Items(I).ToString("X2").ToLower)
            Next I
            Return sb.ToString
        End Function

    End Class

    Public Class ME3PlayerClass
        Public Const CLASS1 As String = "Adept"
        Public Const CLASS2 As String = "Soldier"
        Public Const CLASS3 As String = "Engineer"
        Public Const CLASS4 As String = "Sentinel"
        Public Const CLASS5 As String = "Infiltrator"
        Public Const CLASS6 As String = "Vanguard"

        Private field1_Version1 As Integer
        Private field2_Version2 As Integer
        Private field3_Name As String
        Private field4_Level As Integer
        Private field5_Exp As Single
        Private field6_Promotions As Integer

        Public Members As List(Of Integer)

        Public Sub New(ByVal ClassValue As String)
            Dim ClassFields() As String = Split(ClassValue, ";")
            If ClassFields.Count <> 6 Then Throw New ArgumentException("ClassValue string must be 6 fields.")
            field1_Version1 = Integer.Parse(ClassFields(0))
            field2_Version2 = Integer.Parse(ClassFields(1))
            field3_Name = ClassFields(2)
            field4_Level = Integer.Parse(ClassFields(3))
            field5_Exp = Single.Parse(ClassFields(4))
            field6_Promotions = Integer.Parse(ClassFields(5))
            Me.Members = New List(Of Integer)
        End Sub

        ''' <summary>
        ''' Returns the full class line, including its proper number according to the class name. Ex: class1=20;4;Adept;20;10500000.0000;10
        ''' </summary>
        Public Overrides Function ToString() As String
            'Return the whole damn line, ME3 puts classes in specific order
            Dim strResult As String = ""
            Select Case Name
                Case CLASS1 : strResult = "class1="
                Case CLASS2 : strResult = "class2="
                Case CLASS3 : strResult = "class3="
                Case CLASS4 : strResult = "class4="
                Case CLASS5 : strResult = "class5="
                Case CLASS6 : strResult = "class6="
            End Select
            strResult &= field1_Version1 & ";" & field2_Version2 & ";" & field3_Name & ";" & _
               field4_Level & ";" & field5_Exp.ToString("0.0000") & ";" & field6_Promotions
            Return strResult
        End Function

        Public ReadOnly Property Name As String
            Get
                Return field3_Name
            End Get
        End Property

        Public Function GetLevel() As Integer
            Return field4_Level
        End Function
        Public Sub SetLevel(ByVal intLevel As Integer)
            field4_Level = intLevel
        End Sub

        Public Function GetExperience() As Single
            Return field5_Exp
        End Function
        Public Sub SetExperience(ByVal sngExp As Single)
            field5_Exp = sngExp
        End Sub

        Public Function GetPromotions() As Integer
            Return field6_Promotions
        End Function
        Public Sub SetPromotions(ByVal intPromotions As Integer)
            field6_Promotions = intPromotions
        End Sub

    End Class

    Public Class ME3PlayerChar
        '00 Integer Version1 - should always be 20
        '01 Integer Version2 - should always be 4
        '02 String KitName - internal kit name as seen in Coalesced
        '03 String CharacterName - the name given by the player
        '04 Integer Tint1ID
        '05 Integer Tint2ID
        '06 Integer PatternID
        '07 Integer PatternColorID
        '08 Integer PhongID
        '09 Integer EmissiveID
        '10 Integer SkinToneID
        '11 Integer SecondsPlayed - all time related variables seem to be unused
        '12 Integer TimeStampYear
        '13 Integer TimeStampMonth
        '14 Integer TimeStampDay
        '15 Integer TimeStampSeconds
        '16 String Powers - not a simple string...
        '17 String HotKeys - confirmed to be completely unused
        '18 String Weapons - currently equipped weapons
        '19 String WeaponMods - all weapons ever equipped and their respective equipped mods
        '20 Boolean Deployed - char has been customized at least once
        '21 Boolean LeveledUp - makes the 'level up' arrow appear
        Private fields(21) As String

        Public Sub New(ByVal strvalue As String)
            Dim CharSplit() As String = Split(strvalue, ";")
            fields = CharSplit
        End Sub

        Public Function GetDeployed() As Boolean
            Return Boolean.Parse(fields(20))
        End Function
        Public Sub SetDeployed(ByVal bValue As Boolean)
            fields(20) = bValue.ToString()
        End Sub

        Public ReadOnly Property KitClassName As String
            Get
                If fields(2).Contains(ME3PlayerClass.CLASS1) Then Return ME3PlayerClass.CLASS1
                If fields(2).Contains(ME3PlayerClass.CLASS2) Then Return ME3PlayerClass.CLASS2
                If fields(2).Contains(ME3PlayerClass.CLASS3) Then Return ME3PlayerClass.CLASS3
                If fields(2).Contains(ME3PlayerClass.CLASS4) Then Return ME3PlayerClass.CLASS4
                If fields(2).Contains(ME3PlayerClass.CLASS5) Then Return ME3PlayerClass.CLASS5
                Return ME3PlayerClass.CLASS6
            End Get
        End Property

        Public Overrides Function ToString() As String
            Dim sb As New System.Text.StringBuilder
            For I As Integer = 0 To 20
                sb.Append(fields(I) & ";")
            Next
            sb.Append(fields(21))
            Return sb.ToString()
        End Function

    End Class

    Public Class ME3PlayerCharPower
        Private field1_Name As String
        Private field2_ClassID As Integer
        Private field3_Rank As Single
        Private field4_Rank4a As Byte
        Private field5_Rank4b As Byte
        Private field6_Rank5a As Byte
        Private field7_Rank5b As Byte
        Private field8_Rank6a As Byte
        Private field9_Rank6b As Byte
        Private field10_HotKey As Byte
        Private field11_UsesTalentPoints As Boolean
    End Class

    Public Class ME3PlayerHeader
        Private PID As Long
        Private UID As Long
        Private Auth As String
        Private Auth2 As String
        Private DSNM As String

        Public Sub New(ByVal TextLines As String())
            Dim s() As String
            s = TextLines(0).Split("="c)
            If s.Length = 2 AndAlso s(0) = "PID" Then PID = Long.Parse(s(1).Substring(2), Globalization.NumberStyles.HexNumber)
            s = TextLines(1).Split("="c)
            If s.Length = 2 AndAlso s(0) = "UID" Then UID = Long.Parse(s(1).Substring(2), Globalization.NumberStyles.HexNumber)
            s = TextLines(2).Split("="c)
            If s.Length = 2 AndAlso s(0) = "AUTH" Then Auth = s(1)
            s = TextLines(3).Split("="c)
            If s.Length = 2 AndAlso s(0) = "AUTH2" Then Auth2 = s(1)
            s = TextLines(4).Split("="c)
            If s.Length = 2 AndAlso s(0) = "DSNM" Then DSNM = s(1)
        End Sub

        Public Function GetPID() As Long
            Return PID
        End Function
        Public Sub SetPID(ByVal Value As Long)
            PID = Value
        End Sub

        Public Function GetUID() As Long
            Return UID
        End Function
        Public Sub SetUID(ByVal Value As Long)
            UID = Value
        End Sub

        Public Function GetAuth() As String
            Return Auth
        End Function
        Public Sub SetAuth(ByVal newAuth As String)
            Auth = newAuth
        End Sub

        Public Function GetAuth2() As String
            Return Auth2
        End Function
        Public Sub SetAuth2(ByVal newAuth2 As String)
            Auth2 = newAuth2
        End Sub

        Public Function GetDisplayName() As String
            Return DSNM
        End Function
        Public Sub SetDisplayName(ByVal Name As String)
            DSNM = Name
        End Sub

        Public Function GetLines() As String()
            Dim s() As String = {"PID=0x" & PID.ToString("X8"), "UID=0x" & UID.ToString("X8"), "AUTH=" & Auth, "AUTH2=" & Auth2, "DSNM=" & DSNM}
            Return s
        End Function
    End Class

    Public Class ME3PlayerFaceCodes ' WIP
        Private facecodesline As String
        Public Sub New(ByVal lineFC As String)
            facecodesline = lineFC
        End Sub
        Public Function GetLine() As String
            Return facecodesline
        End Function
    End Class

    Public Class ME3PlayerNewItem ' WIP
        Private newitemline As String
        Public Sub New(ByVal lineNI As String)
            newitemline = lineNI
        End Sub
        Public Function GetLine() As String
            Return newitemline
        End Function
    End Class

    Public Class ME3PlayerBanner
        Private bannerID As Integer

        Public Sub New(ByVal lineBanner As String)
            Dim s() As String = Split(lineBanner, "=")
            bannerID = Integer.Parse(s(1))
        End Sub

        Public Sub SetBannerID(ByVal newID As Integer)
            bannerID = newID
        End Sub

        Public Function GetBannerID() As Integer
            Return bannerID
        End Function

        Public Sub ResetBannerID()
            SetBannerID(0)
        End Sub

        Public Function GetLine() As String
            Return "csreward=" & bannerID
        End Function

    End Class

    Public Class ME3PlayerChallengeStats
        Private completion_list As List(Of Integer)
        Private progress_list As List(Of Integer)
        Private cscompletion_list As List(Of Integer)
        Private cstimestamps_list As List(Of Integer)
        Private cstimestamps2_list As List(Of Integer)
        Private cstimestamps3_list As List(Of Integer)

        Public Sub New(ByVal completion_Str As String, ByVal progress_Str As String, ByVal cscompletion_Str As String,
                       ByVal cstimestamps_Str As String, ByVal cstimestamps2_Str As String, ByVal cstimestamps3_Str As String)
            If Not String.IsNullOrEmpty(completion_Str) Then completion_list = StringToIntegerList(completion_Str)
            If Not String.IsNullOrEmpty(progress_Str) Then progress_list = StringToIntegerList(progress_Str)
            If Not String.IsNullOrEmpty(cscompletion_Str) Then cscompletion_list = StringToIntegerList(cscompletion_Str)
            If Not String.IsNullOrEmpty(cstimestamps_Str) Then cstimestamps_list = StringToIntegerList(cstimestamps_Str)
            If Not String.IsNullOrEmpty(cstimestamps2_Str) Then cstimestamps2_list = StringToIntegerList(cstimestamps2_Str)
            If Not String.IsNullOrEmpty(cstimestamps3_Str) Then cstimestamps3_list = StringToIntegerList(cstimestamps3_Str)
        End Sub

        Public Function GetLines() As List(Of String)
            Dim s As New List(Of String)
            If completion_list IsNot Nothing Then s.Add("Completion=" & IntegerListToString(completion_list))
            If progress_list IsNot Nothing Then s.Add("Progress=" & IntegerListToString(progress_list))
            If cscompletion_list IsNot Nothing Then s.Add("cscompletion=" & IntegerListToString(cscompletion_list))
            If cstimestamps_list IsNot Nothing Then s.Add("cstimestamps=" & IntegerListToString(cstimestamps_list))
            If cstimestamps2_list IsNot Nothing Then s.Add("cstimestamps2=" & IntegerListToString(cstimestamps2_list))
            If cstimestamps3_list IsNot Nothing Then s.Add("cstimestamps3=" & IntegerListToString(cstimestamps3_list))
            Return s
        End Function

        Public Shared Function IntegerListToString(ByVal srcList As List(Of Integer)) As String
            Dim sb As New System.Text.StringBuilder
            sb.Append(srcList(0))
            For I As Integer = 1 To srcList.Count - 1 Step 1
                sb.Append("," & srcList(I))
            Next I
            Return sb.ToString()
        End Function

        Public Shared Function StringToIntegerList(ByVal srcStr As String) As List(Of Integer)
            Dim arrayStr() As String = Split(srcStr, ",")
            Dim intList As New List(Of Integer)
            For Each s As String In arrayStr
                intList.Add(Integer.Parse(s))
            Next s
            Return intList
        End Function

        Public ReadOnly Property ChallengePoints As Integer
            Get
                If completion_list Is Nothing Then Return 0
                Return completion_list(1)
            End Get
        End Property

    End Class

End Namespace