using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace penta
{
    public enum E_SceneChangeUpdateTime
    {
        Default,        // First calling Excute Method
        Excute,         // Same To Default
        ExcuteAsync,
    }
    public interface ISceneChangedUpdate
    {
        public E_SceneChangeUpdateTime E_SceneUpdateTime { get; set; }

        public void Excute();
        public async UniTask ExcuteAsync()
        {
            await UniTask.CompletedTask;
        }
    }
}