param(
    [Parameter(Mandatory = $true)][string]$Name,
    [string]$Method = '',
    [string]$FullType = ''
)

$ErrorActionPreference = 'Stop'
$gameDll = 'D:\steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll'
$assembly = [Reflection.Assembly]::LoadFrom($gameDll)
$type = $assembly.GetType($(if ($FullType) { $FullType } else { 'MegaCrit.Sts2.Core.Models.Relics.' + $Name }))
if ($null -eq $type) { throw "Relic class not found: $Name" }

$codes = @{}
foreach ($field in [Reflection.Emit.OpCodes].GetFields([Reflection.BindingFlags]'Public,Static')) {
    $op = $field.GetValue($null)
    $codes[$op.Value -band 0xffff] = $op
}

$types = @($type) + @($type.GetNestedTypes([Reflection.BindingFlags]'Public,NonPublic'))
foreach ($part in $types) {
    foreach ($member in $part.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Static,Instance,DeclaredOnly')) {
        if ($member.Name -ne 'MoveNext' -and
            ($Method.Length -eq 0 -or $member.Name -ne $Method)) { continue }
        if ($member.Name -eq 'MoveNext' -and $Method.Length -gt 0 -and
            $part.Name -notlike "<$Method>*") { continue }
        $body = $member.GetMethodBody()
        if ($null -eq $body) { continue }
        Write-Output "[$($part.Name).$($member.Name)]"
        $bytes = $body.GetILAsByteArray()
        $index = 0
        while ($index -lt $bytes.Length) {
            $offset = $index
            $key = [int]$bytes[$index++]
            if ($key -eq 254) { $key = 0xfe00 -bor [int]$bytes[$index++] }
            $op = $codes[$key]
            if ($null -eq $op) { throw "Unknown opcode at $offset" }
            $operand = ''
            $size = switch ($op.OperandType.ToString()) {
                'InlineNone' { 0 }
                { $_ -in 'ShortInlineI','ShortInlineBrTarget','ShortInlineVar' } { 1 }
                'InlineVar' { 2 }
                { $_ -in 'InlineI','InlineBrTarget','InlineField','InlineMethod','InlineSig','InlineString','InlineTok','InlineType','ShortInlineR' } { 4 }
                { $_ -in 'InlineI8','InlineR' } { 8 }
                'InlineSwitch' { 4 + 4 * [BitConverter]::ToInt32($bytes, $index) }
                default { throw "Unsupported operand type: $($op.OperandType)" }
            }
            if ($size -gt 0) {
                $raw = [BitConverter]::ToString($bytes, $index, $size)
                $operand = $raw
                if ($op.OperandType.ToString() -in 'InlineField','InlineMethod','InlineTok','InlineType') {
                    try {
                        $token = [BitConverter]::ToInt32($bytes, $index)
                        $operand = $part.Module.ResolveMember($token).ToString()
                    } catch { }
                } elseif ($op.OperandType.ToString() -eq 'InlineString') {
                    try {
                        $token = [BitConverter]::ToInt32($bytes, $index)
                        $operand = '"' + $part.Module.ResolveString($token) + '"'
                    } catch { }
                } elseif ($op.OperandType.ToString() -eq 'InlineI') {
                    $operand = [BitConverter]::ToInt32($bytes, $index)
                } elseif ($op.OperandType.ToString() -eq 'ShortInlineI') {
                    $operand = [int]$bytes[$index]
                    if ($operand -ge 128) { $operand -= 256 }
                }
                $index += $size
            }
            '{0:X4}  {1,-24} {2}' -f $offset, $op.Name, $operand
        }
    }
}
