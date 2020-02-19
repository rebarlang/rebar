define void @create_range_iterator(i32, i32, { i32, i32 }*) {
entry:
  %currentValuePtr = getelementptr inbounds { i32, i32 }* %2, i32 0, i32 0
  %highValuePtr = getelementptr inbounds { i32, i32 }* %2, i32 0, i32 1
  %lowValueDecrement = sub i32 %0, 1
  store i32 %lowValueDecrement, i32* %currentValuePtr
  store i32 %1, i32* %highValuePtr
  ret void
}

define void @range_iterator_next({ i32, i32 }*, { i1, i32 }*) {
entry:
  %rangeCurrentPtr = getelementptr inbounds { i32, i32 }* %0, i32 0, i32 0
  %rangeHighPtr = getelementptr inbounds { i32, i32 }* %0, i32 0, i32 1
  %rangeCurrent = load i32* %rangeCurrentPtr
  %rangeHigh = load i32* %rangeHighPtr
  %rangeCurrentInc = add i32 %rangeCurrent, 1
  store i32 %rangeCurrentInc, i32* %rangeCurrentPtr
  %inRange = icmp slt i32 %rangeCurrentInc, %rangeHigh
  %agg = insertvalue { i1, i32 } undef, i1 %inRange, 0
  %option = insertvalue { i1, i32 } %agg, i32 %rangeCurrentInc, 1
  store { i1, i32 } %option, { i1, i32 }* %1
  ret void
}