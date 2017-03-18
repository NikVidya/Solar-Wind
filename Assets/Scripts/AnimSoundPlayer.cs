using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AnimSoundPlayer : MonoBehaviour {

	[System.Serializable]
	public struct ClipGroup
	{
		[TooltipAttribute("Optional")]
		public string groupName;

		public AudioClip[] clips;
	}
	public ClipGroup[] clipGroups;

	public void PlaySound(int groupIndex){
		AudioClip clip = clipGroups[groupIndex].clips[ Random.Range(0, clipGroups[groupIndex].clips.Length - 1) ];
		GetComponent<AudioSource> ().PlayOneShot (clip);
	}
}
