[CmdletBinding()]
param([Parameter(Mandatory)] [string]$OutputPath)

$ErrorActionPreference = 'Stop'
$target = [System.IO.Path]::GetFullPath($OutputPath)
if (Test-Path -LiteralPath $target) {
    if (Get-ChildItem -LiteralPath $target -Force | Select-Object -First 1) {
        throw 'El destino de certificados debe estar vacío.'
    }
} else {
    New-Item -ItemType Directory -Path $target -Force | Out-Null
}

$dockerPath = $target.Replace('\', '/')
$commands = @(
    'openssl genrsa -out /certs/ca.key 2048',
    'openssl req -x509 -new -key /certs/ca.key -sha256 -days 2 -subj "/CN=SweetSecrets TEN-033 Test CA" -out /certs/ca.crt',
    'openssl genrsa -out /certs/server.key 2048',
    'openssl req -new -key /certs/server.key -subj "/CN=mailpit" -addext "subjectAltName=DNS:mailpit" -out /certs/server.csr',
    'printf "subjectAltName=DNS:mailpit\nextendedKeyUsage=serverAuth\ncrlDistributionPoints=URI:http://web:8080/ten033-ca.crl\n" > /certs/server.ext',
    'openssl x509 -req -in /certs/server.csr -CA /certs/ca.crt -CAkey /certs/ca.key -CAcreateserial -out /certs/server.crt -days 2 -sha256 -extfile /certs/server.ext',
    'printf "[ ca ]\ndefault_ca=test_ca\n[ test_ca ]\ndatabase=/certs/index.txt\nserial=/certs/serial\ncrlnumber=/certs/crlnumber\nprivate_key=/certs/ca.key\ncertificate=/certs/ca.crt\ndefault_md=sha256\ndefault_crl_days=2\n" > /certs/openssl.cnf && touch /certs/index.txt && printf "1000\n" > /certs/serial && printf "1000\n" > /certs/crlnumber',
    'openssl ca -gencrl -config /certs/openssl.cnf -out /certs/ca.crl -batch && openssl crl -hash -noout -in /certs/ca.crl > /certs/ca.crl.hash',
    'rm -f /certs/ca.key /certs/ca.srl /certs/server.csr /certs/server.ext /certs/openssl.cnf /certs/index.txt /certs/serial /certs/crlnumber'
)

foreach ($command in $commands) {
    & docker run --rm -v "${dockerPath}:/certs" --entrypoint sh alpine/openssl:3.5.4 -c $command | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No fue posible generar los certificados efímeros TEN-033.' }
}

Write-Host "Certificados efímeros creados en: $target"
