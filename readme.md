A simple console app to help organize photos into collections by year and month.

Example:
`dotnet run -s "c:\Files\Photos\Unsorted" -d "C:\Files\Photos\ByYear" -m move`

## Parameters

### Source
Argument: `-s`  
The full path of the source folder to read from. All sub-directories will also be processed.  

### Destination
Argument: `-d`  
The full path of the destination folder the processed files will be written to. The year and month folders will automatically be created if they don't already exist.  

### Mode
Argument: `-m`  
The processing mode to use.
Options:  
`-m` (default): Move the files from the source to the destination.  
`-c`: Copy the files from the source to the destination. Useful for trial runs or duplicating the files.  
