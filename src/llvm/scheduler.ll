declare void @schedule(void (i8*)*, i8*)

define void @partial_schedule(i8*, i32*, void (i8*)*) {
entry:
  %3 = atomicrmw sub i32* %1, i32 1 monotonic
  %previousFireCountWasOne = icmp eq i32 %3, 1
  br i1 %previousFireCountWasOne, label %schedule, label %end

schedule:                                         ; preds = %entry
  call void @schedule(void (i8*)* %2, i8* %0)
  br label %end

end:                                              ; preds = %schedule, %entry
  ret void
}

define void @invoke({ void (i8*)*, i8* }) {
entry:
  %functionPtr = extractvalue { void (i8*)*, i8* } %0, 0
  %functionArg = extractvalue { void (i8*)*, i8* } %0, 1
  call void %functionPtr(i8* %functionArg)
  ret void
}