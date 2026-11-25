' dnlib: See LICENSE.txt for more info
'
' A test program in Visual Basic: the things the VB compiler writes to metadata and to a PDB that the C#
' compiler doesn't. Main returns 0 if everything works.

Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic

Namespace VbFeatures
	' A module is a sealed class with only shared members and a StandardModuleAttribute
	Public Module Helpers
		Public Const Greeting As String = "hello"
		Public ReadOnly Created As Date = #1/2/2003 4:05:06 AM#

		' A static local is a field of the class, with a flag field that says if it's initialized
		Public Function NextNumber() As Integer
			Static counter As Integer = 10
			counter += 1
			Return counter
		End Function

		' Optional parameters, one of them a Date: its default value is in a DateTimeConstantAttribute
		Public Function Describe(Optional name As String = "nobody", Optional day As Date = #2/3/2004#) As String
			Return name & " " & day.Year.ToString()
		End Function

		' Local constants of types that are stored in special ways in a PDB
		Public Function Constants() As Integer
			Const Day As Date = #5/6/2007 8:09:10 AM#
			Const Price As Decimal = 12.5D
			Const Title As String = "title"
			Const Nothing1 As Object = Nothing
			Dim total As Integer = Day.Year + CInt(Price) + Title.Length
			If Nothing1 Is Nothing Then total += 1
			Return total
		End Function

		' Exception filter (When) and a handler without a variable
		Public Function Filter(value As Integer) As String
			Try
				If value = 1 Then Throw New InvalidOperationException("one")
				If value = 2 Then Throw New ArgumentException("two")
				Return "none"
			Catch ex As InvalidOperationException When value = 1
				Return "filtered " & ex.Message
			Catch
				Return "caught"
			End Try
		End Function

		' Late binding isn't allowed with Option Strict On, but Select Case on strings, With, ReDim Preserve
		' and Like use the VB runtime helpers that are embedded in this assembly
		Public Function Arrays() As Integer
			Dim numbers(2) As Integer
			numbers(0) = 1
			ReDim Preserve numbers(5)
			numbers(5) = 4
			Dim text As String = "b"
			Select Case text
				Case "a"
					Return -1
				Case "b"
					Return numbers(0) + numbers(5) + numbers.Length
			End Select
			Return -2
		End Function
	End Module

	Public Class Counter
		Implements IDisposable

		' An event with WithEvents and Handles
		Public Event Changed(sender As Object, value As Integer)
		Private WithEvents source As Counter
		Public Property Value As Integer
		Public Property Items As New List(Of String)

		' A parameterized property that isn't the default property
		Public Property Named(name As String) As Integer
			Get
				Return name.Length + Value
			End Get
			Set(newValue As Integer)
				Value = newValue - name.Length
			End Set
		End Property

		Default Public ReadOnly Property Item(index As Integer) As String
			Get
				Return Items(index)
			End Get
		End Property

		Public Sub Watch(other As Counter)
			source = other
		End Sub

		Public Sub Increment()
			Value += 1
			RaiseEvent Changed(Me, Value)
		End Sub

		Private Sub OnSourceChanged(sender As Object, value As Integer) Handles source.Changed
			Me.Value = value * 2
		End Sub

		Public Sub Dispose() Implements IDisposable.Dispose
			Items.Clear()
		End Sub
	End Class

	Public Structure Point
		Public X As Integer
		Public Y As Integer

		Public Shared Operator +(a As Point, b As Point) As Point
			Return New Point With {.X = a.X + b.X, .Y = a.Y + b.Y}
		End Operator

		Public Shared Widening Operator CType(value As Integer) As Point
			Return New Point With {.X = value, .Y = value}
		End Operator
	End Structure

	Public Module Program
		Private failures As Integer

		Private Sub Check(condition As Boolean, what As String)
			If Not condition Then
				failures += 1
				Console.WriteLine("FAILED: " & what)
			End If
		End Sub

		Private Iterator Function Numbers(count As Integer) As IEnumerable(Of Integer)
			For i As Integer = 1 To count
				Yield i * i
			Next
		End Function

		Public Function Main() As Integer
			Check(Helpers.NextNumber() = 11 AndAlso Helpers.NextNumber() = 12, "static local")
			Check(Helpers.Describe() = "nobody 2004", "optional parameters")
			Check(Helpers.Describe("x", Helpers.Created) = "x 2003", "date field")
			Check(Helpers.Constants() = 2007 + 12 + 5 + 1, "local constants")
			Check(Helpers.Filter(0) = "none" AndAlso Helpers.Filter(1) = "filtered one" AndAlso Helpers.Filter(2) = "caught", "exception filter")
			Check(Helpers.Arrays() = 1 + 4 + 6, "arrays and Select Case")

			Dim watched As New Counter()
			Using watcher As New Counter()
				watcher.Watch(watched)
				watched.Increment()
				watched.Increment()
				Check(watched.Value = 2 AndAlso watcher.Value = 4, "WithEvents")
				watcher.Named("abc") = 10
				Check(watcher.Value = 7 AndAlso watcher.Named("ab") = 9, "parameterized property")
				watcher.Items.Add("first")
				Check(watcher(0) = "first", "default property")
			End Using

			Dim sum As Point = New Point With {.X = 1, .Y = 2} + 5
			Check(sum.X = 6 AndAlso sum.Y = 7, "operators")

			Dim total As Integer = 0
			For Each number In Numbers(3)
				total += number
			Next
			Dim addTotal = Function(value As Integer) value + total
			Check(addTotal(1) = 15, "iterator and lambda")

			Return failures
		End Function
	End Module
End Namespace
