declare void @CopyMemory(i8*, i8*, i64)
declare void @free(i8*)
declare noalias i8* @malloc(i32)
declare void @output_string(i8*, i32)

define void @copy_slice_to_pointer({ i8*, i32 }, i8*) {
entry:
  %sourcePtr = extractvalue { i8*, i32 } %0, 0
  %size = extractvalue { i8*, i32 } %0, 1
  %bytesToCopyExtend = sext i32 %size to i64
  call void @CopyMemory(i8* %1, i8* %sourcePtr, i64 %bytesToCopyExtend)
  ret void
}

define void @create_empty_string({ i8*, i32 }*) {
entry:
  %stringAllocationPtrPtr = getelementptr inbounds { i8*, i32 }* %0, i32 0, i32 0
  %stringLengthPtr = getelementptr inbounds { i8*, i32 }* %0, i32 0, i32 1
  %allocationPtr = tail call i8* @malloc(i32 mul (i32 ptrtoint (i8* getelementptr (i8* null, i32 1) to i32), i32 4))
  store i8* %allocationPtr, i8** %stringAllocationPtrPtr
  store i32 0, i32* %stringLengthPtr
  ret void
}

define i8* @create_null_terminated_string_from_slice({ i8*, i32 }) {
entry:
  %stringSliceAllocationPtr = extractvalue { i8*, i32 } %0, 0
  %stringSliceLength = extractvalue { i8*, i32 } %0, 1
  %nullTerminatedStringLength = add i32 %stringSliceLength, 1
  %mallocsize = mul i32 %nullTerminatedStringLength, ptrtoint (i8* getelementptr (i8* null, i32 1) to i32)
  %nullTerminatedStringAllocationPtr = tail call i8* @malloc(i32 %mallocsize)
  %nullBytePtr = getelementptr i8* %nullTerminatedStringAllocationPtr, i32 %stringSliceLength
  call void @copy_slice_to_pointer({ i8*, i32 } %0, i8* %nullTerminatedStringAllocationPtr)
  store i8 0, i8* %nullBytePtr
  ret i8* %nullTerminatedStringAllocationPtr
}

define void @drop_string({ i8*, i32 }*) {
entry:
  %stringAllocationPtrPtr = getelementptr inbounds { i8*, i32 }* %0, i32 0, i32 0
  %stringAllocationPtr = load i8** %stringAllocationPtrPtr
  tail call void @free(i8* %stringAllocationPtr)
  ret void
}

define void @output_string_slice({ i8*, i32 }) {
entry:
  %stringBufferPtr = extractvalue { i8*, i32 } %0, 0
  %stringSize = extractvalue { i8*, i32 } %0, 1
  call void @output_string(i8* %stringBufferPtr, i32 %stringSize)
  ret void
}

define void @string_from_slice({ i8*, i32 }, { i8*, i32 }*) {
entry:
  %sliceAllocationPtr = extractvalue { i8*, i32 } %0, 0
  %sliceSize = extractvalue { i8*, i32 } %0, 1
  %mallocsize = mul i32 %sliceSize, ptrtoint (i8* getelementptr (i8* null, i32 1) to i32)
  %allocationPtr = tail call i8* @malloc(i32 %mallocsize)
  %bytesToCopyExtend = sext i32 %sliceSize to i64
  call void @CopyMemory(i8* %allocationPtr, i8* %sliceAllocationPtr, i64 %bytesToCopyExtend)
  %agg = insertvalue { i8*, i32 } undef, i8* %allocationPtr, 0
  %string = insertvalue { i8*, i32 } %agg, i32 %sliceSize, 1
  store { i8*, i32 } %string, { i8*, i32 }* %1
  ret void
}

define { i8*, i32 } @string_to_slice_ret({ i8*, i32 }*) {
entry:
  %stringAllocationPtrPtr = getelementptr inbounds { i8*, i32 }* %0, i32 0, i32 0
  %stringSizePtr = getelementptr inbounds { i8*, i32 }* %0, i32 0, i32 1
  %stringAllocationPtr = load i8** %stringAllocationPtrPtr
  %stringSize = load i32* %stringSizePtr
  %agg = insertvalue { i8*, i32 } undef, i8* %stringAllocationPtr, 0
  %slice = insertvalue { i8*, i32 } %agg, i32 %stringSize, 1
  ret { i8*, i32 } %slice
}

define void @string_to_slice({ i8*, i32 }*, { i8*, i32 }*) {
entry:
  %sliceReference = call { i8*, i32 } @string_to_slice_ret({ i8*, i32 }* %0)
  store { i8*, i32 } %sliceReference, { i8*, i32 }* %1
  ret void
}

define void @string_append({ i8*, i32 }*, { i8*, i32 }) {
entry:
  %stringAllocationPtrPtr = getelementptr inbounds { i8*, i32 }* %0, i32 0, i32 0
  %stringSizePtr = getelementptr inbounds { i8*, i32 }* %0, i32 0, i32 1
  %stringAllocationPtr = load i8** %stringAllocationPtrPtr
  %stringSize = load i32* %stringSizePtr
  %sliceSize = extractvalue { i8*, i32 } %1, 1
  %appendedSize = add i32 %stringSize, %sliceSize
  %mallocsize = mul i32 %appendedSize, ptrtoint (i8* getelementptr (i8* null, i32 1) to i32)
  %newAllocationPtr = tail call i8* @malloc(i32 %mallocsize)
  %stringSlice = call { i8*, i32 } @string_to_slice_ret({ i8*, i32 }* %0)
  call void @copy_slice_to_pointer({ i8*, i32 } %stringSlice, i8* %newAllocationPtr)
  %newAllocationOffsetPtr = getelementptr i8* %newAllocationPtr, i32 %stringSize
  call void @copy_slice_to_pointer({ i8*, i32 } %1, i8* %newAllocationOffsetPtr)
  tail call void @free(i8* %stringAllocationPtr)
  store i8* %newAllocationPtr, i8** %stringAllocationPtrPtr
  store i32 %appendedSize, i32* %stringSizePtr
  ret void
}

define void @string_concat({ i8*, i32 }, { i8*, i32 }, { i8*, i32 }*) {
entry:
  %sliceSize0 = extractvalue { i8*, i32 } %0, 1
  %sliceSize1 = extractvalue { i8*, i32 } %1, 1
  %concatSize = add i32 %sliceSize0, %sliceSize1
  %mallocsize = mul i32 %concatSize, ptrtoint (i8* getelementptr (i8* null, i32 1) to i32)
  %concatAllocationPtr = tail call i8* @malloc(i32 %mallocsize)
  %concatAllocationOffsetPtr = getelementptr i8* %concatAllocationPtr, i32 %sliceSize0
  %stringAllocationPtrPtr = getelementptr inbounds { i8*, i32 }* %2, i32 0, i32 0
  %stringSizePtr = getelementptr inbounds { i8*, i32 }* %2, i32 0, i32 1
  call void @copy_slice_to_pointer({ i8*, i32 } %0, i8* %concatAllocationPtr)
  call void @copy_slice_to_pointer({ i8*, i32 } %1, i8* %concatAllocationOffsetPtr)
  store i8* %concatAllocationPtr, i8** %stringAllocationPtrPtr
  store i32 %concatSize, i32* %stringSizePtr
  ret void
}

define void @string_slice_to_string_split_iterator({ i8*, i32 }, { { i8*, i32 }, i8* }*) {
entry:
  %initialStringPtr = extractvalue { i8*, i32 } %0, 0
  %agg = insertvalue { { i8*, i32 }, i8* } undef, { i8*, i32 } %0, 0
  %stringSplitIterator = insertvalue { { i8*, i32 }, i8* } %agg, i8* %initialStringPtr, 1
  store { { i8*, i32 }, i8* } %stringSplitIterator, { { i8*, i32 }, i8* }* %1
  ret void
}

define void @string_split_iterator_next({ { i8*, i32 }, i8* }*, { i1, { i8*, i32 } }*) {
entry:
  %stringSplitIterator = load { { i8*, i32 }, i8* }* %0
  %stringSliceRef = extractvalue { { i8*, i32 }, i8* } %stringSplitIterator, 0
  %stringSliceBeginPtr = extractvalue { i8*, i32 } %stringSliceRef, 0
  %stringSliceLength = extractvalue { i8*, i32 } %stringSliceRef, 1
  %stringLimitPtr = getelementptr i8* %stringSliceBeginPtr, i32 %stringSliceLength
  %initialPtr = extractvalue { { i8*, i32 }, i8* } %stringSplitIterator, 1
  br label %findSplitBeginLoopBegin

findSplitBeginLoopBegin:                          ; preds = %advanceSplitBegin, %entry
  %splitBeginPtr = phi i8* [ %initialPtr, %entry ], [ %splitBeginIncrementPtr, %advanceSplitBegin ]
  %2 = ptrtoint i8* %splitBeginPtr to i64
  %3 = ptrtoint i8* %stringLimitPtr to i64
  %4 = sub i64 %2, %3
  %splitBeginPtrDiff = sdiv exact i64 %4, ptrtoint (i8* getelementptr (i8* null, i32 1) to i64)
  %isSplitBeginPtrPastEnd = icmp sge i64 %splitBeginPtrDiff, 0
  br i1 %isSplitBeginPtrPastEnd, label %returnNone, label %checkSplitBeginForSpace

checkSplitBeginForSpace:                          ; preds = %findSplitBeginLoopBegin
  %splitBeginChar = load i8* %splitBeginPtr
  %isSplitBeginSpace = icmp eq i8 %splitBeginChar, 32
  br i1 %isSplitBeginSpace, label %advanceSplitBegin, label %findSplitEndLoopBeginBlock

advanceSplitBegin:                                ; preds = %checkSplitBeginForSpace
  %splitBeginIncrementPtr = getelementptr i8* %splitBeginPtr, i32 1
  br label %findSplitBeginLoopBegin

findSplitEndLoopBeginBlock:                       ; preds = %advanceSplitEnd, %checkSplitBeginForSpace
  %splitLength = phi i32 [ 1, %checkSplitBeginForSpace ], [ %splitLengthIncrement, %advanceSplitEnd ]
  %splitEndPtr = getelementptr i8* %splitBeginPtr, i32 %splitLength
  %5 = ptrtoint i8* %splitEndPtr to i64
  %6 = ptrtoint i8* %stringLimitPtr to i64
  %7 = sub i64 %5, %6
  %splitEndPtrDiff = sdiv exact i64 %7, ptrtoint (i8* getelementptr (i8* null, i32 1) to i64)
  %isSplitEndPtrPastEnd = icmp sge i64 %splitEndPtrDiff, 0
  br i1 %isSplitEndPtrPastEnd, label %returnSome, label %checkSplitEndForSpace

checkSplitEndForSpace:                            ; preds = %findSplitEndLoopBeginBlock
  %splitEndChar = load i8* %splitEndPtr
  %isSplitEndSpace = icmp eq i8 %splitEndChar, 32
  br i1 %isSplitEndSpace, label %returnSome, label %advanceSplitEnd

advanceSplitEnd:                                  ; preds = %checkSplitEndForSpace
  %splitLengthIncrement = add i32 %splitLength, 1
  br label %findSplitEndLoopBeginBlock

returnSome:                                       ; preds = %checkSplitEndForSpace, %findSplitEndLoopBeginBlock
  %agg = insertvalue { i8*, i32 } undef, i8* %splitBeginPtr, 0
  %slice = insertvalue { i8*, i32 } %agg, i32 %splitLength, 1
  %option = insertvalue { i1, { i8*, i32 } } { i1 true, { i8*, i32 } undef }, { i8*, i32 } %slice, 1
  br label %end

returnNone:                                       ; preds = %findSplitBeginLoopBegin
  br label %end

end:                                              ; preds = %returnNone, %returnSome
  %option1 = phi { i1, { i8*, i32 } } [ %option, %returnSome ], [ zeroinitializer, %returnNone ]
  %finalPtr = phi i8* [ %splitEndPtr, %returnSome ], [ %splitBeginPtr, %returnNone ]
  store { i1, { i8*, i32 } } %option1, { i1, { i8*, i32 } }* %1
  %stringSplitIteratorCurrentPtr = getelementptr inbounds { { i8*, i32 }, i8* }* %0, i32 0, i32 1
  store i8* %finalPtr, i8** %stringSplitIteratorCurrentPtr
  ret void
}

define void @string_clone({ i8*, i32 }*, { i8*, i32 }*) {
entry:
  %stringSliceRef = call { i8*, i32 } @string_to_slice_ret({ i8*, i32 }* %0)
  call void @string_from_slice({ i8*, i32 } %stringSliceRef, { i8*, i32 }* %1)
  ret void
}