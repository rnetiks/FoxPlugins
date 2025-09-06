using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiledRenderer.UIElements
{
    public class Notification : MonoBehaviour
    {
        private static List<Message> _messages = new List<Message>();
        private void Update()
        {
            for (int index = 0; index < _messages.Count; index++)
            {
                var message = _messages[index];
                message.TimeLeft -= Time.deltaTime;
                _messages[index] = message;
                if (message.TimeLeft <= 0)
                {
                    _messages.Remove(message);
                }
            }
        }
        
        private void OnGUI()
        {
            var sw = Screen.width;
            var sh = Screen.height;
            
            
        }
        
        public static void AddMessage(string text, float duration, Color color)
        {
            _messages.Add(new Message(text, duration, color));
        }
        
        private struct Message
        {
            public string Text;
            public float TimeLeft;
            public Color Color;
            
            public Message(string text, float timeLeft, Color color)
            {
                Text = text;
                TimeLeft = timeLeft;
                Color = color;
            }
        }
    }
}