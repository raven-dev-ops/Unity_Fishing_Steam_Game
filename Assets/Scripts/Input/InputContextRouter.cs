using System;
using UnityEngine;

namespace RavenDevOps.Fishing.Input
{
    public sealed class InputContextRouter : MonoBehaviour
    {
        [SerializeField] private InputContext _activeContext = InputContext.UI;

        public InputContext ActiveContext => _activeContext;

        public event Action<InputContext, InputContext> ContextChanged;

        public void SetContext(InputContext context)
        {
            if (context == _activeContext)
            {
                return;
            }

            var previous = _activeContext;
            _activeContext = context;
            ContextChanged?.Invoke(previous, context);
        }
    }
}
