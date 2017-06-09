# XamarinFormsSwipeCell

현재 iOS 용으로만 구현이 되어 있음

코드 참조 :
1. Custoizing ViewCell : https://developer.xamarin.com/guides/xamarin-forms/application-fundamentals/custom-renderer/viewcell/
2. Working with Tables and Cells (xamarin ios) : https://developer.xamarin.com/guides/ios/user_interface/tables/
3. xamarin ios ContextActionsCell : https://github.com/xamarin/Xamarin.Forms/blob/master/Xamarin.Forms.Platform.iOS/ContextActionCell.cs
4. xamarin ios ContextScrollViewDelegate : https://github.com/xamarin/Xamarin.Forms/blob/master/Xamarin.Forms.Platform.iOS/ContextScrollViewDelegate.cs


*주의

- iOS Custom Swipe Cell 구현시 ListView 의 CachingStrategy="RecycleElement" 를 선택하는 경우
- 최종적으로 ContexActionCell 의 Update 가 마지막에 실행되어 ScrollerView 안에 또다시 ScrollerView를 실행하는 결과가 됨.
