$server = "localhost"
$port = 12201

#$json = '{ "version": "1.1", "host": "127.0.0.1", "short_message": "This is a sample log message", "level": "DEBUG" }'
$json = '{"t":{"$date":"2023-08-17T16:16:07.672+00:00"}, "s":"I", "msg":"test112233"}'
$bytes = [text.encoding]::UTF8.GetBytes($json);

$clientTcp = New-Object System.Net.Sockets.TCPClient($server, $port)
$stream = $clientTcp.GetStream();
$stream.Write($bytes, 0, $bytes.Length);
$clientTcp.close()

$clientUdp = New-Object System.Net.Sockets.UdpClient(0)
$clientUdp.Send($bytes, $bytes.length, $server, $port)
$clientUdp.Close()

Write-Host("Sent to TCP and UDP: ", $json);

#$ipep = new-object net.ipendpoint([net.ipaddress]::any, 0)
#$receive = $client.receive([ref]$ipep)

#Write-Output ([text.encoding]::ascii.getstring($receive))

