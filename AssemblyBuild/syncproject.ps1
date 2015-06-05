$namespace = "http://schemas.microsoft.com/developer/msbuild/2003"
$ns = @{ p = $namespace}

$source = $args[0]
$dest = $args[1]
$relativeFilePath = $args[2]

#creating a list of the reimplemented files so we don't try to use the ones from Imagine.Common since they won't work in WinRT
$rewrittenFiles = New-Object System.Collections.Generic.List[System.String]

$sourceXml = [xml](get-content $source)
$targetXml = [xml](get-content $dest)
$originalTargetXml = $targetXml.CloneNode($true)

$itemGroup = $targetXml | Select-Xml -XPath "//p:ItemGroup[p:Compile]" -Namespace $ns

#remove all nodes that are links so we can readd them later
$childrenToRemove= New-Object System.Collections.Generic.List[System.Xml.XmlNode]
foreach($childNode in $itemGroup.Node.ChildNodes)
{
    if ([string]::IsNullOrEmpty($childNode.Link))
    {
        $rewrittenFiles.Add($childNode.Include)
    }
    
	if ($childNode.Link -notlike '*Internal*' )
    {
        $childrenToRemove.Add($childNode)
        #$itemGroup.Node.RemoveChild($childNode)
    }
}
foreach($remove in $childrenToRemove)
{
    $itemGroup.Node.RemoveChild($remove) | Out-Null
}

$moreNodes = $sourceXml | Select-Xml -XPath "//p:ItemGroup/p:Compile" -Namespace $ns

#add all files from the original source to the destination csproj creating a link and a relative path
foreach($node in ($sourceXml | Select-Xml -XPath "//p:ItemGroup/p:Compile" -Namespace $ns))
{
    if ( $rewrittenFiles -notcontains $node.Node.Include)
    {
        $link = $targetXml.CreateNode('element', 'Link', $namespace)
        $linkTarget = $targetXml.CreateTextNode($node.Node.Include)
        $link.AppendChild($linkTarget) | Out-Null

        $compile = $targetXml.CreateNode('element', 'Compile', $namespace)

        $relativePath = $relativeFilePath + $node.Node.Include;
        $compile.SetAttribute("Include", $relativePath)
        $compile.AppendChild($link) | Out-Null

        $itemGroup.Node.AppendChild($compile) | Out-Null
    }
}

if (diff $targetXml.OuterXml $originalTargetXml.OuterXml)
{
	Write-Host Updating $dest
	$targetXml.Save($dest)
}
else
{
	Write-Host $dest is up to date.
}