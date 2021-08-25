#if ENABLE_FIRESTORE
using System.Collections.Generic;
using System.Linq;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FirestoreTest
{
	public class CloudFirestore : MonoBehaviour
	{
		[SerializeField] private InputField emailField;
		[SerializeField] private InputField nameField;
		[SerializeField] private InputField bioField;
		[SerializeField] private Text msg;
		
		[FormerlySerializedAs("ErrorPanel")] [SerializeField] private GameObject errorPanel;

		[SerializeField] private InputField emailFieldUpdate;
		[SerializeField] private InputField nameFieldUpdate;
		[SerializeField] private InputField bioFieldUpdate;
		[SerializeField] private Text msgUpdate;

		[SerializeField] private InputField emailFieldFetch;
		[SerializeField] private InputField nameFieldFetch;
		[SerializeField] private InputField bioFieldFetch;
		[SerializeField] private Text timeStamp;
		[SerializeField] private Text msgFetch;
	
		[FormerlySerializedAs("UpdatePanel")] [SerializeField] private GameObject updatePanel;
		[FormerlySerializedAs("ReadPanel")] [SerializeField] private GameObject readPanel;
		
		private FirebaseFirestore db;
		private static string userEmail;
		
		private void Start()
		{
			db = FirebaseFirestore.DefaultInstance;
		}

		public void AddNewEmail ()
		{
			msg.text = "Logs:";

			if (emailField.text.Length == 0) { msg.text = "Email is Missing !"; return; }
			if (nameField.text.Length == 0) { msg.text = "Name is Missing !"; return; }
			if (bioField.text.Length == 0) { msg.text = "Bio is Missing !"; return; }

			msg.text = "Adding Data ...";

			var docRef = db.Collection("users").Document(emailField.text);
			var user = new User
			{
				Name = nameField.text,
				Email = emailField.text,
				Bio = bioField.text,
				TimeStamp = FieldValue.ServerTimestamp
			};

			docRef.SetAsync(user).ContinueWithOnMainThread(task =>
			{
				if (task.IsCanceled)
				{
					msg.text = "An Error Occurred !";
					return;
				}

				if (task.IsFaulted)
				{
					msg.text = "Add Data Failed Failed !";
					return;
				}

				if (task.IsCompleted)
				{
					msg.text = "New Data Added, Now You can read and update data using: " + emailField.text;
					userEmail = emailField.text;
					emailFieldUpdate.text = userEmail;
				}
			});
		}

		public void UpdateButtonClick()
		{
			ReadData(true);
		}

		public void ReadButtonClick ()
		{
			ReadData();
		}
		
		private void ReadData(bool updateData = false)
		{
			msg.text = "Reading Data ...";
			var usersRef = db.Collection("users");
			usersRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
			{
				if (task.IsCanceled) { msg.text = "An Error Occurred !"; return; }
				if (task.IsFaulted) { msg.text = "Add Data Failed Failed !"; return; }
				
				var snapshot = task.Result;
				if (snapshot.Documents == null || !snapshot.Documents.Any())
				{
					msg.text = $"Data is null or empty";
					return;
				}
				
				foreach (var document in snapshot.Documents)
				{
					Debug.Log($"User: {document.Id}");
					var documentDictionary = document.ToDictionary();
					Debug.Log($"name {documentDictionary["Name"]}");
					Debug.Log($"email {documentDictionary["Email"]}");
					Debug.Log($"bio {documentDictionary["Bio"]}");
					Debug.Log($"timestamp {documentDictionary["TimeStamp"]}");
				}

				ShowUserInfo(snapshot.Documents.Last(), updateData);
			});
		}

		private void ShowUserInfo(DocumentSnapshot firstData, bool updateData)
		{
			if (!updateData)
			{
				ShowUserInfo(firstData.ToDictionary(), nameFieldFetch, emailFieldFetch, bioFieldFetch);
				readPanel.SetActive(true);
			}
			else
			{
				ShowUserInfo(firstData.ToDictionary(), nameFieldUpdate, emailFieldUpdate, bioFieldUpdate);
				updatePanel.SetActive (true);
			}
		}

		private void ShowUserInfo(IReadOnlyDictionary<string, object> firstData, InputField nameUI, InputField emailUI, InputField bioUI)
		{
			nameUI.text = $"{firstData["Name"]}";
			emailUI.text = $"{firstData["Email"]}";
			bioUI.text = $"{firstData["Bio"]}";
			msg.text = $"name {firstData["Name"]} \n" +
			           $"email {firstData["Email"]} \n" +
			           $"bio {firstData["Bio"]}";
		}

		public void UpdateEmailData ()
		{
			msg.text = "Updating Data ...";
			if (emailFieldUpdate.text.Length == 0) { msg.text = "Email is Missing !"; return; }

			var nameRef = db.Collection("users").Document(emailFieldUpdate.text);
			var updates = new Dictionary<string, object>
			{
				{"name", nameFieldUpdate.text},
				{"bio", bioFieldUpdate.text}
			};

			nameRef.UpdateAsync(updates).ContinueWithOnMainThread(task2 =>
			{
				if (task2.IsCanceled)
				{
					msg.text = "An Error Occurred !";
					return;
				}

				if (task2.IsFaulted)
				{
					msg.text = "Add Data Failed Failed !";
					return;
				}

				if (task2.IsCompleted)
				{
					msg.text = "Data Updated for: " + userEmail;
				}
			});
		}
	}
}
#endif