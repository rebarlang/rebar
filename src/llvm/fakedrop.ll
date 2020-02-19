declare void @fake_drop(i32)

define void @fakedrop_create(i32, { i32 }*) {
entry:
  %2 = getelementptr inbounds { i32 }* %1, i32 0, i32 0
  store i32 %0, i32* %2 
  ret void
}

define void @fakedrop_drop({ i32 }*) {
entry:
  %1 = getelementptr inbounds { i32 }* %0, i32 0, i32 0
  %2 = load i32* %1
  call void @fake_drop(i32 %2)
  ret void
}