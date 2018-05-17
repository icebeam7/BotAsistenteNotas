using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Luis.Models;

namespace BotAsistenteNotas.Dialogs
{
    [LuisModel("reemplaza-el-model-id", "reemplaza-la-llave-de-subscripcion", domain: "westus.api.cognitive.microsoft.com")]
    [Serializable]
    public class DialogoNota : LuisDialog<object>
    {
        private readonly Dictionary<string, Nota> ListaNotas = new Dictionary<string, Nota>();

        public const string TituloNota = "Nueva nota";
        public const string EntidadTituloNota = "TituloNota";

        public bool BuscarNota(LuisResult result, out Nota nota)
        {
            nota = null;
            string tituloBusqueda;
            EntityRecommendation titulo;

            if (result.TryFindEntity(EntidadTituloNota, out titulo))
                tituloBusqueda = titulo.Entity;
            else
                tituloBusqueda = TituloNota;

            return this.ListaNotas.TryGetValue(tituloBusqueda, out nota);
        }

        public bool BuscarNota(string tituloNota, out Nota nota)
        {
            bool busqueda = this.ListaNotas.TryGetValue(tituloNota, out nota);
            return busqueda;
        }

        [LuisIntent("")]
        public async Task Nada(IDialogContext context, LuisResult result)
        {
            string message = $"Bot Asistente de Notas. Puedo entender peticiones para crear, eliminar y leer notas. \n\n Intención detectada: " + string.Join(", ", result.Intents.Select(i => i.Intent));
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("EliminarNota")]
        public async Task EliminarNota(IDialogContext context, LuisResult result)
        {
            Nota nota;
            if (BuscarNota(result, out nota))
            {
                this.ListaNotas.Remove(nota.Titulo);
                await context.PostAsync($"Nota {nota.Titulo} eliminada");
            }
            else
            {
                PromptDialog.Text(context, After_DeleteTitlePrompt, "¿Cuál es el título de la nota que deseas eliminar?");
            }
        }

        private async Task After_DeleteTitlePrompt(IDialogContext context, IAwaitable<string> result)
        {
            Nota nota;
            string tituloEliminar = await result;
            bool notaEncontrada = this.ListaNotas.TryGetValue(tituloEliminar, out nota);

            if (notaEncontrada)
            {
                this.ListaNotas.Remove(nota.Titulo);
                await context.PostAsync($"Nota {nota.Titulo} eliminada");
            }
            else
            {
                await context.PostAsync($"Nota {tituloEliminar} no encontrada.");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("LeerNota")]
        public async Task EncontrarNota(IDialogContext context, LuisResult result)
        {
            Nota nota;
            if (BuscarNota(result, out nota))
            {
                await context.PostAsync($"**{nota.Titulo}**: {nota.Texto}.");
            }
            else
            {
                string mensaje = "Aquí está la lista de todas las notas: \n\n";
                foreach (KeyValuePair<string, Nota> entrada in ListaNotas)
                {
                    Nota registro = entrada.Value;
                    mensaje += $"**{registro.Titulo}**: {registro.Texto}.\n\n";
                }
                await context.PostAsync(mensaje);
            }

            context.Wait(MessageReceived);
        }

        private Nota notaNueva;
        private string tituloActual;

        [LuisIntent("CrearNota")]
        public Task RegistrarNota(IDialogContext context, LuisResult result)
        {
            EntityRecommendation titulo;
            if (!result.TryFindEntity(EntidadTituloNota, out titulo))
            {
                PromptDialog.Text(context, After_TitlePrompt, "¿Cuál es el título de la nueva nota?");
            }
            else
            {
                var note = new Nota() { Titulo = titulo.Entity };
                notaNueva = this.ListaNotas[note.Titulo] = note;

                PromptDialog.Text(context, After_TextPrompt, "¿Cuál es el contenido de tu nota?");
            }

            return Task.CompletedTask;
        }

        private async Task After_TitlePrompt(IDialogContext context, IAwaitable<string> result)
        {
            EntityRecommendation title;
            tituloActual = await result;

            if (tituloActual != null)
            {
                title = new EntityRecommendation(type: EntidadTituloNota) { Entity = tituloActual };
            }
            else
            {
                title = new EntityRecommendation(type: EntidadTituloNota) { Entity = TituloNota };
            }

            var nota = new Nota() { Titulo = title.Entity };
            notaNueva = this.ListaNotas[nota.Titulo] = nota;

            PromptDialog.Text(context, After_TextPrompt, "¿Cuál es el contenido de tu nota?");
        }

        private async Task After_TextPrompt(IDialogContext context, IAwaitable<string> result)
        {
            notaNueva.Texto = await result;
            await context.PostAsync($"Nota creada **{this.notaNueva.Titulo}**, contenido: \"{this.notaNueva.Texto}\".");
            context.Wait(MessageReceived);
        }


        public DialogoNota()
        {

        }

        public DialogoNota(ILuisService service)
            : base(service)
        {
        }

        [Serializable]
        public sealed class Nota : IEquatable<Nota>
        {
            public string Titulo { get; set; }
            public string Texto { get; set; }

            public override string ToString()
            {
                return $"[{this.Titulo} : {this.Texto}]";
            }

            public bool Equals(Nota other)
            {
                return other != null
                    && this.Texto == other.Texto
                    && this.Titulo == other.Titulo;
            }

            public override bool Equals(object other)
            {
                return Equals(other as Nota);
            }

            public override int GetHashCode()
            {
                return this.Titulo.GetHashCode();
            }
        }
    }
}