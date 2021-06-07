function Set-Icon{
    param(
        $Icon_Name,
        $Window_Name_Expression
    )

    $Icon_Name = "$Icon_Name"

    $WM_SETICON = 0x0080
    $Icon = $Api::ExtractIcon(0, "$Icon_Name", 0);

    echo "$Icon_Name иконка найдена? $($Icon -ne 0), количество окон: $((get-process|where{$_.mainWindowTItle -match $Window_Name_Expression}).length)"

    get-process|where{$_.mainWindowTItle -match $Window_Name_Expression}|%{$Api::SendMessage($_.MainWindowHandle,$WM_SETICON,0,$Icon)}
    get-process|where{$_.mainWindowTItle -match $Window_Name_Expression}|Select-Object MainWindowHandle
    $WM_SETICON
    0
    $Icon
}

function GetAll1C{
    return Out-String -InputObject (get-process|Where {$_.MainWindowTitle}|Where-Object{$_.Name -match "1cv8"}|Select-Object ProcessName, MainWindowTitle)
}

$Api = Add-Type -MemberDefinition @'
[DllImport("shell32.dll")]
public static extern IntPtr ExtractIcon(IntPtr hInst,string file,int index);        
[DllImport("user32.dll")]
public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
'@ -Name 'Api' -PassThru

#global variables
$previous = ""

while ($true) {

    $current = GetAll1C
    if ($previous -ne $current) { # if 
        # refresh cache
        ie4uinit.exe -ClearIconCache
        ie4uinit.exe -show

        # change icons
        Set-Icon -Icon_Name "C:\Users\eegorov.KURGANMK\ps1\changeicons\icons\уппппп.ico"   -Window_Name_Expression "Управление производственным предприятием"
        Set-Icon -Icon_Name "зупппп2.ico"  -Window_Name_Expression "Зарплата и управление персоналом"
        Set-Icon -Icon_Name "lp.ico"       -Window_Name_Expression "1С:Предприятие - Печать этикеток"
        Set-Icon -Icon_Name "stol.ico"     -Window_Name_Expression "1С:Предприятие - Столовая"
        $previous = GetAll1C
    }
    else{
        echo "=="
    }
    
    # sleep
    Start-Sleep -Seconds 10
}