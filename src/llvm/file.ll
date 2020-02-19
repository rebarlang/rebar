declare i32 @CloseHandle(i8*)
declare i8* @CreateFileA(i8*, i32, i32, i8*, i32, i32, i8*)
declare i32 @ReadFile(i8*, i8*, i32, i32*, i8*)
declare i32 @WriteFile(i8*, i8*, i32, i32*, i8*)

declare void @free(i8*)

; from string module
declare i8* @create_null_terminated_string_from_slice({ i8*, i32 })
declare void @create_empty_string({ i8*, i32 }*)
declare void @drop_string({ i8*, i32 }*)
declare void @string_append({ i8*, i32 }*, { i8*, i32 })

define void @open_file_handle({ i8*, i32 }, { i1, { i8* } }*) {
entry:
  %nullTerminatedStringtPtr = call i8* @create_null_terminated_string_from_slice({ i8*, i32 } %0)
  %fileHandleIsSomePtr = getelementptr inbounds { i1, { i8* } }* %1, i32 0, i32 0
  %fileHandleInnerValuePtr = getelementptr inbounds { i1, { i8* } }* %1, i32 0, i32 1
  %fileHandleInnerValueFileHandlePtr = getelementptr inbounds { i8* }* %fileHandleInnerValuePtr, i32 0, i32 0
  %fileHandle = call i8* @CreateFileA(i8* %nullTerminatedStringtPtr, i32 -1073741824, i32 0, i8* null, i32 4, i32 128, i8* null)
  tail call void @free(i8* %nullTerminatedStringtPtr)
  store i1 true, i1* %fileHandleIsSomePtr
  store i8* %fileHandle, i8** %fileHandleInnerValueFileHandlePtr
  ret void
}

define void @read_line_from_file_handle({ i8* }*, { i1, { i8*, i32 } }*) {
entry:
  %stringPtr = alloca { i8*, i32 }
  %carriageReturnPtr = alloca i8
  %byteReadPtr = alloca i8
  %bytesReadPtr = alloca i32
  %nonEmptyStringPtr = alloca i1
  %seenCRPtr = alloca i1
  store i8 13, i8* %carriageReturnPtr
  store i1 false, i1* %seenCRPtr
  store i1 false, i1* %nonEmptyStringPtr
  call void @create_empty_string({ i8*, i32 }* %stringPtr)
  br label %loopStart

loopStart:                                        ; preds = %appendByte, %byteIsCR, %entry
  %hFilePtr = getelementptr inbounds { i8* }* %0, i32 0, i32 0
  %hFile = load i8** %hFilePtr
  %readFileResult = call i32 @ReadFile(i8* %hFile, i8* %byteReadPtr, i32 1, i32* %bytesReadPtr, i8* null)
  %readFileResultBool = icmp ne i32 %readFileResult, 0
  %bytesRead = load i32* %bytesReadPtr
  %zeroBytesRead = icmp eq i32 %bytesRead, 0
  %eof = and i1 %readFileResultBool, %zeroBytesRead
  br i1 %eof, label %loopEnd, label %handleByte

handleByte:                                       ; preds = %loopStart
  %byteRead = load i8* %byteReadPtr
  %byteReadIsCR = icmp eq i8 %byteRead, 13
  br i1 %byteReadIsCR, label %byteIsCR, label %byteIsNotCR

byteIsCR:                                         ; preds = %handleByte
  store i1 true, i1* %seenCRPtr
  br label %loopStart

byteIsNotCR:                                      ; preds = %handleByte
  %byteIsLF = icmp eq i8 %byteRead, 10
  %seenCR = load i1* %seenCRPtr
  %newLine = and i1 %byteIsLF, %seenCR
  br i1 %newLine, label %loopEnd, label %notNewLine

notNewLine:                                       ; preds = %byteIsNotCR
  br i1 %seenCR, label %appendCR, label %appendByte

appendCR:                                         ; preds = %notNewLine
  %agg = insertvalue { i8*, i32 } undef, i8* %carriageReturnPtr, 0
  %slice = insertvalue { i8*, i32 } %agg, i32 1, 1
  call void @string_append({ i8*, i32 }* %stringPtr, { i8*, i32 } %slice)
  br label %appendByte

appendByte:                                       ; preds = %appendCR, %notNewLine
  %agg1 = insertvalue { i8*, i32 } undef, i8* %byteReadPtr, 0
  %slice2 = insertvalue { i8*, i32 } %agg1, i32 1, 1
  call void @string_append({ i8*, i32 }* %stringPtr, { i8*, i32 } %slice2)
  store i1 true, i1* %nonEmptyStringPtr
  store i1 false, i1* %seenCRPtr
  br label %loopStart

loopEnd:                                          ; preds = %byteIsNotCR, %loopStart
  %optionStringIsSomePtr = getelementptr inbounds { i1, { i8*, i32 } }* %1, i32 0, i32 0
  %nonEmptyString3 = load i1* %nonEmptyStringPtr
  br i1 %nonEmptyString3, label %nonEmptyString, label %emptyString

nonEmptyString:                                   ; preds = %loopEnd
  store i1 true, i1* %optionStringIsSomePtr
  %optionStringInnerValuePtr = getelementptr inbounds { i1, { i8*, i32 } }* %1, i32 0, i32 1
  %string = load { i8*, i32 }* %stringPtr
  store { i8*, i32 } %string, { i8*, i32 }* %optionStringInnerValuePtr
  ret void

emptyString:                                      ; preds = %loopEnd
  store i1 false, i1* %optionStringIsSomePtr
  call void @drop_string({ i8*, i32 }* %stringPtr)
  ret void
}

define void @write_string_to_file_handle({ i8* }*, { i8*, i32 }) {
entry:
  %fileHandlePtr = getelementptr inbounds { i8* }* %0, i32 0, i32 0
  %fileHandle = load i8** %fileHandlePtr
  %stringSliceAllocationPtr = extractvalue { i8*, i32 } %1, 0
  %stringSliceLength = extractvalue { i8*, i32 } %1, 1
  %bytesWrittenPtr = alloca i32
  %writeFileResult = call i32 @WriteFile(i8* %fileHandle, i8* %stringSliceAllocationPtr, i32 %stringSliceLength, i32* %bytesWrittenPtr, i8* null)
  ret void
}

define void @drop_file_handle({ i8* }*) {
entry:
  %fileHandlePtr = getelementptr inbounds { i8* }* %0, i32 0, i32 0
  %fileHandle = load i8** %fileHandlePtr
  %closeHandleResult = call i32 @CloseHandle(i8* %fileHandle)
  ret void
}