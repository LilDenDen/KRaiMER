using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Crosstales.RTVoice;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections;

namespace OpenAI
{
    public class ChatGPT : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Button button2;
        [SerializeField] private Text textArea;
        [SerializeField] private Text textArea2;
        [SerializeField] private bool isTest;

        string path;

        private OpenAIApi openai = new OpenAIApi();
        private string promptFormat;

        private string formattedText = " ";

        private bool isRunning = false; // is speaking

        private bool isComplete = false;

        private string currentScript;
        private int scriptIndex;
        private bool paused = false;


        private void Start()
        {
            intializePaths();


            textArea.color = Color.white;

            SendReply("Write a long script of a Seinfeld episode about cars, " + promptFormat);

            Speaker.OnSpeakComplete += isSpeakingCheck;             //Event to check when speaker is done talking

            button.onClick.AddListener(ScriptPause);
        }

        private void intializePaths()
        {
            path = Application.dataPath;
            promptFormat = File.ReadAllText(path + "\\textFiles\\prompt.txt");       //String that tells GPT how to format script

        }
        private async void SendReply(string input)
        {

            Scene scene = new Scene();             // initializing the constructor for some reason, i guess it gets mad otherwise

            string aiReply;            // variable for response



            if (isTest)
            {
                string textFile = File.ReadAllText(path + "\\textFiles\\scriptfile.txt");
                currentScript = textFile;
                scene = JsonConvert.DeserializeObject<Scene>(textFile);


            }
            // Complete the instruction
            else
            {

                var completionResponse = await openai.CreateCompletion(new CreateCompletionRequest()
                {
                    Prompt = input,
                    Model = "text-davinci-003",
                    MaxTokens = 2000
                });
                //Format for Text Box
                formattedText = $"\n{completionResponse.Choices[0].Text}\n";
                textArea.text = formattedText;

                aiReply = completionResponse.Choices[0].Text;
                currentScript = aiReply;

                //Create a Scene Object, properties in class at bottom
                scene = JsonConvert.DeserializeObject<Scene>(aiReply);
            }

            //pass the scene to the dialogue manager
            StartCoroutine(DialogueManager(scene));


            button.enabled = true;
        }

        public void Speak(string tts, string name)
        {
            isRunning = true;
            if (name == "Jerry")
            {
                Speaker.Speak(tts, null, Speaker.VoiceForName("Microsoft David Desktop"), true, 1.5f, 1.4f);
            }
            else if (name == "Elaine")
            {
                Speaker.Speak(tts, null, Speaker.VoiceForName("Microsoft Hazel Desktop"), true, 1.5f);
            }
            else
            {
                Speaker.Speak(tts, null, Speaker.VoiceForName("Microsoft David Desktop"), true, 1.5f, .3f);
            }



        }
        IEnumerator DialogueManager(Scene scene)
        {
            //for now ignore chars and episode and just looping through the ScriptLine object also at bottom
            foreach (Actor actor in scene.script)
            {
                Debug.Log(actor.name + " (" + actor.emotion + "): " + actor.dialogue);
                scriptIndex += 1;


                //while loop to check if audio currently playing
                while (isRunning == true)
                {
                    yield return null;
                }



                textArea2.text = (actor.name + " (" + actor.emotion + "): " + actor.dialogue + $"\n");


                Speak(actor.dialogue, actor.name);
            }

            isComplete = true;
        }



        private async void ScriptPause()
        {
            button.enabled = false;

            // just for testing
            while (!isComplete) await Task.Delay(100);





            if (paused == false)
            {
                string summarizePrompt = "Summarize this script explain the key things that happen and explain where exactly we left off, " + currentScript;
                Debug.Log(summarizePrompt);
                var completionResponse = await openai.CreateCompletion(new CreateCompletionRequest()
                {
                    Prompt = summarizePrompt,
                    Model = "text-davinci-003",
                    MaxTokens = 1000
                });
                string response = completionResponse.Choices[0].Text;
                File.WriteAllText(path + "\\textFiles\\summary.txt", response);
                Debug.Log(response);

                paused = true;
                button.enabled = true;
            }
            else
            {
                Scene scene = new Scene();

                string summary = File.ReadAllText(path + "\\textFiles\\summary.txt");
                string summarizePrompt = "Write a long script of a Seinfeld episode continuing from where we left off, So far a summary of the story is,  " + summary + ", "  + promptFormat;
                Debug.Log(summarizePrompt);
                var completionResponse = await openai.CreateCompletion(new CreateCompletionRequest()
                {
                    Prompt = summarizePrompt,
                    Model = "text-davinci-003",
                    MaxTokens = 1000
                });
                string aiReply = completionResponse.Choices[0].Text;
                scene = JsonConvert.DeserializeObject<Scene>(aiReply);
                StartCoroutine(DialogueManager(scene));

                paused = false;
                button.enabled = true;
            }


        }


        private void isSpeakingCheck(Crosstales.RTVoice.Model.Wrapper wrapper)
        {

            isRunning = false;


        }

        private async Task<string> GenGPTCall(string prompt)
        {

            
            var completionResponse = await openai.CreateCompletion(new CreateCompletionRequest()
            {
                Prompt = prompt,
                Model = "text-davinci-003",
                MaxTokens = 1000
            });
            return completionResponse.Choices[0].Text;
        }

    }



    [System.Serializable]
    public class Scene
    {
        public string episode;
        public List<string> Characters;
        public List<Actor> script;
    }
    public class Actor
    {
        public string name;
        public string emotion;
        public string dialogue;
    }
}

