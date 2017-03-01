using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeechBubbleBehaviour : StateMachineBehaviour {

	private int BUBBLE_IN_STATE = Animator.StringToHash("Base Layer.bubble_in");
	private int BUBBLE_OUT_STATE = Animator.StringToHash("Base Layer.bubble_out");

	 // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	//override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
	//override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateExit is called when a transition ends and the state machine finishes evaluating this state
	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		DialogBubble bubbleScript = animator.gameObject.GetComponent<DialogBubble> ();
		if (bubbleScript != null) {
			if (stateInfo.fullPathHash == BUBBLE_IN_STATE) {
				bubbleScript.HandleBubbleInFinished ();
			} else if (stateInfo.fullPathHash == BUBBLE_OUT_STATE) {
				bubbleScript.HandleBubbleOutFinished ();
			}
		}
	}

	// OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
	//override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
	//override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}
}
