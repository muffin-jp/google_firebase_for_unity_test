namespace Gravitons.UI.Modal
{
    using System;
    
    public delegate void CallbackWithParams (params object[] items);

    /// <summary>
    /// Base wrapper class for button properties
    /// </summary>
    public class ModalButton
    {
        /// <summary>
        /// The text to show on the button
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The callback function that will be called when the button is clicked
        /// </summary>
        public Action Callback { get; set; }
        
        public CallbackWithParams CallbackWithParams { get; set; }

        public object[] Params { get; set; }

        /// <summary>
        /// When set to true, modal will close automatically if the button is clicked
        /// </summary>
        public bool CloseModalOnClick
        {
            get { return m_CloseModalOnClick; }
            set { m_CloseModalOnClick = value; }
        }
        private bool m_CloseModalOnClick = true;
    }
}