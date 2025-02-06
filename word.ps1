# Load the Word application object
$word = New-Object -ComObject Word.Application
$word.Visible = $false

# Open the document
$documentPath = "H:\DocumentIndexerScript\output_test1\opt_test1.docx"
$document = $word.Documents.Open($documentPath)

# Start of blank page removal
$totalPages = $document.ComputeStatistics([Microsoft.Office.Interop.Word.WdStatistic]::wdStatisticPages)
Write-Host "Total number of pages before: $totalPages"

# Loop through each page to check for blank pages
for ($N = $totalPages; $N -gt 0; $N--) {
    $range = $document.GoTo([Microsoft.Office.Interop.Word.WdGoToItem]::wdGoToPage, [Microsoft.Office.Interop.Word.WdGoToDirection]::wdGoToAbsolute, $N)
    $nextPageStart = $document.GoTo([Microsoft.Office.Interop.Word.WdGoToItem]::wdGoToPage, [Microsoft.Office.Interop.Word.WdGoToDirection]::wdGoToAbsolute, $N + 1).Start

    if ($nextPageStart -eq $null) {
        $range.End = $document.Content.End
    } else {
        $range.End = $nextPageStart - 1
    }

    # Check if the range is empty (blank page)
    # Exclude headers and footers from the check
    $isBlank = $true
    foreach ($section in $document.Sections) {
        $sectionRange = $section.Range
        $sectionRange.Start = $range.Start
        $sectionRange.End = $range.End

        # Check for content in the main body, headers, and footers
        if ($sectionRange.Text -match "\S" -or `
            $section.Headers([Microsoft.Office.Interop.Word.WdHeaderFooterIndex]::wdHeaderFooterPrimary).Range.Text -match "\S" -or `
            $section.Footers([Microsoft.Office.Interop.Word.WdHeaderFooterIndex]::wdHeaderFooterPrimary).Range.Text -match "\S") {
            $isBlank = $false
            break
        }
    }

    if ($isBlank) {
        # Remove manual page breaks or section breaks causing the blank page
        $range.Find.ClearFormatting()
        $range.Find.Text = "^m"  # Match manual page breaks
        $range.Find.Replacement.Text = ""
        $range.Find.Execute($null, $null, $null, $null, $null, $null, $null, $null, $null, $null, [Microsoft.Office.Interop.Word.WdReplace]::wdReplaceAll)

        $range.Find.Text = "^b"  # Match section breaks
        $range.Find.Replacement.Text = ""
        $range.Find.Execute($null, $null, $null, $null, $null, $null, $null, $null, $null, $null, [Microsoft.Office.Interop.Word.WdReplace]::wdReplaceAll)

        Write-Host "Blank page removed from page $N"
    }
}

# Update the table of contents if it exists
if ($document.TablesOfContents.Count -gt 0) {
    $document.TablesOfContents.Item(1).Update()
    Write-Host "Table of Contents updated."
}

# Save the document as PDF
$pdfPath = "H:\DocumentIndexerScript\output_test1\opt_test1.pdf"
$document.SaveAs([ref] $pdfPath, [ref] [Microsoft.Office.Interop.Word.WdSaveFormat]::wdFormatPDF)

# Close the document and release COM objects
$document.Close()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($document) | Out-Null
$word.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null

Write-Host "Document closed and COM object released."
