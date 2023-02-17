function search {
    param (
        [string]$prefix,
        [int]$length,
        [string]$low = "a",
        [string]$high = "z"
    )
    if ($high -lt $low) {
        $t = $low
        $low = $high
        $high = $t
    }
    Select-String -Path .\words\scroggle -Pattern ^$prefix `
    | %{ $_.Line } `
    | ?{ $_.Length -eq $length -and $_ -gt $low -and $_ -lt $high }
}