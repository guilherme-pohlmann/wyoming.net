$udp = New-Object System.Net.Sockets.UdpClient(5005)
$ep = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any,0)
while ($true) {
  $data = $udp.Receive([ref]$ep)
  [Text.Encoding]::UTF8.GetString($data)
}
