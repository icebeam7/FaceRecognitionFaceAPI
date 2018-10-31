using FaceRecognitionFaceAPI.Classes;
using FaceRecognitionFaceAPI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FaceRecognitionFaceAPI.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FacePage : ContentPage
    {
        static MemoryStream streamCopy;
        MyPerson Person;

        public FacePage()
        {
            InitializeComponent();

            Person = new MyPerson();
        }

        private async void btnGroup_Clicked(object sender, EventArgs e)
        {
            var resultado = await FaceAPIService.CreateGroup(txtNombreGrupo.Text);

            lblResultado.Text = resultado ? "Grupo creado correctamente" : "Error al crear el grupo";

            Person.GroupId = FaceAPIService.group;
        }

        private async void btnAgregar_Clicked(object sender, EventArgs e)
        {
            var resultado = await FaceAPIService.CreatePerson(txtNombrePersona.Text);
            lblResultado.Text = "Person ID: " + resultado;
            Person.PersonId = resultado;
        }

        private async void btnFoto_Clicked(object sender, EventArgs e)
        {
            var usarCamara = ((Button)sender).Text.Contains("Cámara");
            var file = await ImageService.TakePic(usarCamara);
            
            lblResultado.Text = "---";

            imgFoto.Source = ImageSource.FromStream(() => {
                var stream = file.GetStream();
                streamCopy = new MemoryStream();
                stream.CopyTo(streamCopy);
                stream.Seek(0, SeekOrigin.Begin);
                file.Dispose();
                return stream;
            });
        }

        private async void btnObtenerInfo_Clicked(object sender, EventArgs e)
        {
            if (streamCopy != null)
            {
                var face = await FaceAPIService.DetectFaces(streamCopy);

                if (face != null)
                {
                    string resultado = $"ID: {face.FaceID}\n Edad: {face.FaceAttributes.Age}\n Felicidad: {face.FaceAttributes.Emotion.Happiness * 100} %";
                    lblResultado.Text = resultado;
                }
                else
                    lblResultado.Text = "Error al detectar rostro";
            }
            else
                lblResultado.Text = "---No has seleccionado una imagen---";
        }

        private async void btnRegistrar_Clicked(object sender, EventArgs e)
        {
            if (streamCopy != null)
            {
                var resultado = await FaceAPIService.RegisterPerson(Person.PersonId, streamCopy);
                lblResultado.Text = "Persisted Face ID: " + resultado;
            }
            else
                lblResultado.Text = "---No has seleccionado una imagen---";
        }

        private async void btnIdentificar_Clicked(object sender, EventArgs e)
        {
            if (streamCopy != null)
            {
                var resultado = await FaceAPIService.IdentifyPerson(streamCopy);
                lblResultado.Text = "La foto pertenece a : " + resultado;
            }
            else
                lblResultado.Text = "---No has seleccionado una imagen---";
        }

        private async void btnEntrenar_Clicked(object sender, EventArgs e)
        {
            var resultado = await FaceAPIService.TrainGroup();
            lblResultado.Text = "Entrenamiento aceptado";
        }
    }
}