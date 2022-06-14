﻿Imports System.IO
Imports DWSIM.Drawing.SkiaSharp.GraphicObjects
Imports DWSIM.DrawingTools.Point
Imports DWSIM.Interfaces.Enums.GraphicObjects
Imports DWSIM.UnitOperations.UnitOperations
Imports SkiaSharp

Namespace UnitOperations

    Public Class HydroelectricTurbine

        Inherits CleanEnergyUnitOpBase

        Private ImagePath As String = ""

        Private Image As SKImage

        <Xml.Serialization.XmlIgnore> Public f As EditingForm_HydroelectricTurbine

        Public Overrides Property Prefix As String = "HT-"

        Public Property Efficiency As Double = 75

        Public Property StaticHead As Double = 1.0

        Public Property VelocityHead As Double = 1.0

        Public Property InletVelocity As Double = 1.0

        Public Property OutletVelocity As Double = 0.5

        Public Property TotalHead As Double = 0.0

        Public Property GeneratedPower As Double = 0.0

        Public Overrides Function GetDisplayName() As String
            Return "Hydroelectric Turbine"
        End Function

        Public Overrides Function GetDisplayDescription() As String
            Return "Hydroelectric Turbine"
        End Function

        Public Sub New()

            MyBase.New()

        End Sub


        Public Overrides Sub Draw(g As Object)

            Dim canvas As SKCanvas = DirectCast(g, SKCanvas)

            If Image Is Nothing Then

                ImagePath = SharedClasses.Utility.GetTempFileName()
                My.Resources.icons8_hydroelectric.Save(ImagePath)

                Using streamBG = New FileStream(ImagePath, FileMode.Open)
                    Using bitmap = SKBitmap.Decode(streamBG)
                        Image = SKImage.FromBitmap(bitmap)
                    End Using
                End Using

                Try
                    File.Delete(ImagePath)
                Catch ex As Exception
                End Try

            End If

            Using p As New SKPaint With {.IsAntialias = GlobalSettings.Settings.DrawingAntiAlias, .FilterQuality = SKFilterQuality.High}
                canvas.DrawImage(Image, New SKRect(GraphicObject.X, GraphicObject.Y, GraphicObject.X + GraphicObject.Width, GraphicObject.Y + GraphicObject.Height), p)
            End Using

        End Sub

        Public Overrides Sub CreateConnectors()

            Dim w, h, x, y As Double
            w = GraphicObject.Width
            h = GraphicObject.Height
            x = GraphicObject.X
            y = GraphicObject.Y

            Dim myIC1 As New ConnectionPoint

            myIC1.Position = New Point(x, y + h / 2)
            myIC1.Type = ConType.ConIn
            myIC1.Direction = ConDir.Right

            Dim myOC1 As New ConnectionPoint
            myOC1.Position = New Point(x + w, y + h / 2)
            myOC1.Type = ConType.ConOut
            myOC1.Direction = ConDir.Right

            Dim myOC2 As New ConnectionPoint
            myOC2.Position = New Point(x + w / 2, y + h)
            myOC2.Type = ConType.ConOut
            myOC2.Direction = ConDir.Down
            myOC2.Type = ConType.ConEn

            With GraphicObject.InputConnectors
                If .Count = 1 Then
                    .Item(0).Position = New Point(x, y + h / 2)
                Else
                    .Add(myIC1)
                End If
                .Item(0).ConnectorName = "Water Inlet"
            End With

            With GraphicObject.OutputConnectors
                If .Count = 2 Then
                    .Item(0).Position = New Point(x + w, y + h / 2)
                    .Item(1).Position = New Point(x + w / 2, y + h)
                Else
                    .Add(myOC1)
                    .Add(myOC2)
                End If
                .Item(0).ConnectorName = "Water Outlet"
                .Item(1).ConnectorName = "Power Outlet"
            End With

            Me.GraphicObject.EnergyConnector.Active = False

        End Sub

        Public Overrides Sub PopulateEditorPanel(ctner As Object)

        End Sub
        Public Overrides Sub DisplayEditForm()

            If f Is Nothing Then
                f = New EditingForm_HydroelectricTurbine With {.SimObject = Me}
                f.ShowHint = GlobalSettings.Settings.DefaultEditFormLocation
                f.Tag = "ObjectEditor"
                Me.FlowSheet.DisplayForm(f)
            Else
                If f.IsDisposed Then
                    f = New EditingForm_HydroelectricTurbine With {.SimObject = Me}
                    f.ShowHint = GlobalSettings.Settings.DefaultEditFormLocation
                    f.Tag = "ObjectEditor"
                    Me.FlowSheet.DisplayForm(f)
                Else
                    f.Activate()
                End If
            End If

        End Sub

        Public Overrides Sub UpdateEditForm()

            If f IsNot Nothing Then
                If Not f.IsDisposed Then
                    If f.InvokeRequired Then f.BeginInvoke(Sub() f.UpdateInfo())
                End If
            End If

        End Sub

        Public Overrides Sub CloseEditForm()

            If f IsNot Nothing Then
                If Not f.IsDisposed Then
                    f.Close()
                    f = Nothing
                End If
            End If

        End Sub

        Public Overrides Function ReturnInstance(typename As String) As Object

            Return New HydroelectricTurbine

        End Function

        Public Overrides Function GetIconBitmap() As Object

            Return My.Resources.icons8_hydroelectric

        End Function

        Public Overrides Function CloneXML() As Object

            Dim obj As ICustomXMLSerialization = New HydroelectricTurbine()
            obj.LoadData(Me.SaveData)
            Return obj

        End Function

        Public Overrides Function CloneJSON() As Object

            Throw New NotImplementedException()

        End Function

        Public Overrides Function LoadData(data As System.Collections.Generic.List(Of System.Xml.Linq.XElement)) As Boolean

            Dim ci As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture

            XMLSerializer.XMLSerializer.Deserialize(Me, data)

            Return True

        End Function

        Public Overrides Function SaveData() As System.Collections.Generic.List(Of System.Xml.Linq.XElement)

            Dim elements As System.Collections.Generic.List(Of System.Xml.Linq.XElement) = XMLSerializer.XMLSerializer.Serialize(Me)
            Dim ci As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture

            Return elements

        End Function

        Public Overrides Sub Calculate(Optional args As Object = Nothing)

            Dim msin = GetInletMaterialStream(0)
            Dim msout = GetOutletMaterialStream(0)

            Dim esout = GetOutletEnergyStream(1)

            Dim eta = Efficiency / 100.0

            Dim q = msin.GetVolumetricFlow()

            Dim rho = msin.GetPhase("Liquid").Properties.density.GetValueOrDefault()

            Dim g = 9.8

            Dim hs = StaticHead

            Dim hv = (InletVelocity ^ 2 - OutletVelocity ^ 2) / (2 * g)

            VelocityHead = hv

            TotalHead = hs + hv

            Dim p = eta * rho * g * (hs + hv) * q

            GeneratedPower = p / 1000.0

            esout.EnergyFlow = GeneratedPower

            msout.Clear()
            msout.ClearAllProps()

            msout.AssignFromPhase(Enums.PhaseLabel.Mixture, msin, True)
            msout.SetPressure(msin.GetPressure)
            msout.SetMassEnthalpy(msin.GetMassEnthalpy() - GeneratedPower / msin.GetMassFlow())
            msout.SetFlashSpec("PH")

            msout.AtEquilibrium = False

        End Sub

    End Class

End Namespace