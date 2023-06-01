using Goatrello.Models;
using Goatrello.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
using NuGet.Packaging;
using SQLitePCL;
using System;
using System.Text;
using Activity = Goatrello.Models.Activity;

namespace Goatrello.Data
{
	public static class GoatrelloDataInitializer
	{
		public static async void Seed(IApplicationBuilder applicationBuilder, IWebHostEnvironment environment)
		{
			GoatrelloDataContext context = applicationBuilder.ApplicationServices.CreateScope()
				.ServiceProvider.GetRequiredService<GoatrelloDataContext>();


			IWebHostEnvironment hostingEnvironment = environment;

			//Random used for random generation
			Random random = new Random();

			////Delete the database if you need to apply a new Migration
			context.Database.EnsureDeleted();
			//Create the database if it does not exist and apply the Migration
			context.Database.Migrate();
			//Create Users
			var userManager = applicationBuilder.ApplicationServices.CreateScope()
				.ServiceProvider.GetRequiredService<UserManager<User>>();

			if (userManager.FindByEmailAsync("goat@goat.ca").Result == null)
			{
				User user = new User
				{
					UserName = "goat@goat.ca",
					Email = "goat@goat.ca",
					FirstName = "Test",
					LastName = "Administrator",
					IsSiteAdmin = true
				};

				IdentityResult result = userManager.CreateAsync(user, "goatgoat").Result;
				if (result.Succeeded)
				{
					//userManager.AddToRoleAsync(user, "Admin").Wait();
				}
			}

			string[] firstNames = new string[]
			{
				"James", "Mary", "Robert", "Patricia", "John", "Jennifer", "Michael", "Linda", "David", "Elizabeth"
			};
			string[] lastNames = new string[] { "Smith", "Johnson", "Williams", "Brown", "Jones" };

			for (int i = 0; i < firstNames.Length; i++)
			{
				if (userManager.FindByEmailAsync($"testuser{i}@outlook.com").Result == null)
				{
					User user = new User
					{
						UserName = $"testuser{i}@outlook.com",
						Email = $"testuser{i}@outlook.com",
						FirstName = firstNames[i],
						LastName = lastNames[random.Next(0, lastNames.Length - 1)],
						IsSiteAdmin = false,
						IsArchived = false
					};

					IdentityResult result = userManager.CreateAsync(user, "goatuser").Result;
					if (result.Succeeded)
					{
						//userManager.AddToRoleAsync(user, "User").Wait();
					}
				}
			}

			//Board 
			//int arrays holding valid Primary Key values that can be randomly assingn as foreign keys
			int[] adminIDs = context.UserLists.Select(u => u.Id).ToArray();
			int[] memberIDs = context.UserLists.Select(u => u.Id).ToArray();
			int[] observIDs = context.UserLists.Select(u => u.Id).ToArray();
			int[] hiddenIDs = context.UserLists.Select(u => u.Id).ToArray();

			if (!context.Boards.Any())
			{
				context.Boards.AddRange(
				new Board
				{
					Title = "Priorities",
					Labels = new LabelList(),
					IsArchived = false,
					IsPrivate = false,
					Administrators = new UserList(),
					Members = new UserList(),
					Observers = new UserList(),
					HiddenFrom = new UserList(),
				},
				new Board
				{
					Title = "Scheduling",
					Labels = new LabelList(),
					IsArchived = false,
					IsPrivate = false,
					Administrators = new UserList(),
					Members = new UserList(),
					Observers = new UserList(),
					HiddenFrom = new UserList(),
				},
				new Board
				{
					Title = "ProjectDev",
					Labels = new LabelList(),
					IsArchived = false,
					IsPrivate = false,
					Administrators = new UserList(),
					Members = new UserList(),
					Observers = new UserList(),
					HiddenFrom = new UserList(),
				});
				context.SaveChanges();
			}


			int[] userListIDs = context.UserLists.Select(a => a.Id).ToArray();
			int UserListIDCount = userListIDs.Count();

			int[] userIDs = context.Users.Select(a => a.Id).ToArray();
			int UserIDCount = userIDs.Count();

			if (!context.UserListUsers.Any())
			{
				for (int i = 0; i < 10; i++)
				{
					UserListUser userList = new UserListUser
					{
						ListId = userListIDs[i],
						UserId = userIDs[i]
					};
					context.UserListUsers.Add(userList);
				}
				context.SaveChanges();
			}



			int[] BoardIDs = context.Boards.Select(u => u.Id).ToArray();
			string[] ListNames = { "Flagged", "On Deck", "Daily Processing Goal", "Currently Working ON", "Review", "To Be Published" };
			//Listing
			if (!context.Listings.Any())
			{
				//Templates Listing
				foreach (int i in BoardIDs)
				{
					Listing l = new Listing
					{
						BoardId = i,
						Title = "TemplateListing",
						Observers = new UserList(),
						HiddenFrom = new UserList(),
						IsPrivate = false,
						IsTemplate = true
					};
					context.Listings.Add(l);
				}

				foreach (int i in BoardIDs)
				{
					foreach (string name in ListNames)
					{
						Listing l = new Listing
						{
							BoardId = i,
							Title = name,
							Observers = new UserList(),
							HiddenFrom = new UserList(),
							IsPrivate = false,
							IsTemplate = false
						};
						context.Listings.Add(l);
					}
				}


				context.SaveChanges();
			}


			int[] ListingIDs = context.Listings.Where(l => l.IsTemplate == false).Select(u => u.Id).ToArray();
			string[] cardNames = { "Client - cbd2000", "trial field 5", "HUK - G J GOFF - Foxburrow Farm - THE VILLAGE - SetID:19199", "Fresh Fields - Apple"
				, "Dalrymple - Guthries", "Kubota - Kitahime", "Kubota - Soybean", "Ad Marvelous - Field 14", "CC -Crop Care - Boettger - Boettger - KR Town"
				, "CropTech -Crop Tech Solutions - 13_14", "Godsey - 4AgTech - Jamie Madsen"};
			//Cards
			if (!context.Cards.Any())
			{
				//Template cards
				foreach (Listing l in context.Listings.Where(l => l.IsTemplate == true))
				{

					string client = cardNames[random.Next(0, cardNames.Length - 1)];
					var contact = context.Users.ToArray()[random.Next(0, context.Users.Count() - 1)];
					Card c = new Card
					{
						ListingId = l.Id,
						Title = "Template Example",
						Description = "General Information\r\n" +
						$"Full Provider Name: \r\n" +
						$"Provider Account Name (Portal): \r\n" +
						$"Contact Person: \r\n" +
						$"Sensors:",
						Labels = new LabelList(),
						DueDate = (random.Next(1, 20) > 10) ? DateTime.Now.AddDays(random.Next(-10, 40)) : null,
						IsDueDateComplete = (random.Next(1, 20) > 12),
						IsArchived = false
					};
					context.Cards.Add(c);
					context.SaveChanges();
				}

				//Actual cards
				foreach (int i in ListingIDs)
				{
					foreach (string n in cardNames)
					{
						if (random.Next(0, 20) > 10)
						{
							string client = cardNames[random.Next(0, cardNames.Length - 1)];
							var contact = context.Users.ToArray()[random.Next(0, context.Users.Count() - 1)];
							Card c = new Card
							{
								ListingId = i,
								Title = n,
								Description = "General Information\r\n" +
								$"Full Provider Name: {n}\r\n" +
								$"Provider Account Name (Portal): {n}\r\n" +
								$"Contact Person: {contact.DisplayName}\r\n" +
								$"Sensors:17A-003 - Plexagro\r\n" +
								$"19B-032 - L3G Orbely\r\n" +
								$"19B-038 - AGD\r\n" +
								$"19B-0046 - Agroideas\r\n" +
								$"21B-0070 - Glimax Services\r\n" +
								$"21B-0071 - Glimax Services",
								Labels = new LabelList(),
								DueDate = (random.Next(1, 20) > 10) ? DateTime.Now.AddDays(random.Next(-10, 40)) : null,
								IsDueDateComplete = (random.Next(1, 20) > 12),
								IsArchived = false
							};
							context.Cards.Add(c);
							context.SaveChanges();
						}

					}
				}
				context.SaveChanges();
			}

			//Labels
			string[] pastelColors = { "#1F9AEE", "#1CBCD2", "#199588", "#8FC057", "#FC5934", "#FD9731", "#cc00ff", "#bf8040", "#00cc00" };

			if (!context.Labels.Any())
			{
				for (int i = 0; i < 9; i++)
				{
					Label l = new Label
					{
						Title = $"Demo Label {i}",
						//Color = pastelColors[new Random().Next(pastelColors.Length)],
						Color = pastelColors[i],
						//Color = String.Format("#{0:X6}", random.Next(0x1000000)),
						IsArchived = false,
					};
					context.Labels.Add(l);
				}
				context.SaveChanges();


				var boards = context.Boards
					.Include(b => b.Labels).ThenInclude(b => b.Labels)
					.Include(b => b.Listings).ThenInclude(b => b.Cards).ThenInclude(b => b.Labels).ThenInclude(b => b.Labels)
					.ToArray();

				int k = 0;
				foreach (var label in context.Labels)
				{
					LabelListLabel l = new LabelListLabel
					{
						LabelId = label.Id,
						ListId = boards[k].LabelsId
					};
					boards[k++].Labels.Labels.Add(label);
					context.LabelListLabels.Add(l);
					context.SaveChanges();
					if (k == boards.Length) k = 0;
				}

				var cards = context.Cards
					.Include(c => c.Listing)
					.ThenInclude(c => c.Board)
					.ThenInclude(c => c.Labels)
					.ThenInclude(c => c.Labels)
					.Include(c => c.Labels)
					.ThenInclude(c => c.Labels);

				foreach (var card in cards)
				{
					//var labels = context.Labels.Take(5);


					var labels = context.LabelListLabels.Include(l => l.Label).Where(l => l.ListId == card.Listing.Board.Labels.Id);


					foreach (var label in labels)
					{
						LabelListLabel l = new LabelListLabel
						{
							LabelId = label.Label.Id,
							ListId = card.LabelsId
						};
						card.Labels.Labels.Add(label.Label);
						context.LabelListLabels.Add(l);
						context.SaveChanges();
					}
				}
			}


			//Server files
			if (!context.ServerFiles.Any())
			{
				string uploads = Path.Combine(hostingEnvironment.WebRootPath, "uploads");

				if (Directory.GetFiles(uploads).Length == 0)
				{
					//Get the soil optix logo
					string images = Path.Combine(hostingEnvironment.WebRootPath, "images");

					Stream defaultFile = new FileStream(
						Path.Combine(images, "SoilOptix_Logo.png"),
						FileMode.Open, FileAccess.Read
						);

					var uniqueFileName = Guid.Empty;
					string uploadPath = Path.Combine(uploads, uniqueFileName.ToString());

					FileStream newfile = new FileStream(uploadPath, FileMode.Create);

					defaultFile.CopyTo(newfile);
					newfile.Close();
					defaultFile.Close();
				}

				string filePath = Directory.GetFiles(uploads).FirstOrDefault();

				foreach (var card in context.Cards)
				{

					ServerFile file = new ServerFile()
					{
						Id = card.Id,
						Title = card.Title + " Attachment.png",
						FileType = "image/png",
						FileGuid = Guid.Parse(Path.GetFileName(filePath)),
						UserId = 1
					};

					context.ServerFiles.Add(file);
				}
				context.SaveChanges();
			}

			//Attachments
			if (!context.Attachments.Any())
			{
				foreach (var card in context.Cards)
				{

					Attachment attach = new Attachment()
					{
						CardId = card.Id,
						ServerFileId = card.Id,
						IsArchived = false,
					};
					card.Attachments.Add(attach);
				}
				context.SaveChanges();
			}

			//links
			if (!context.Links.Any())
			{
				foreach (var card in context.Cards)
				{
					Link check = new Link()
					{
						CardId = card.Id,
						Uri = new Uri("https://goatrello.azurewebsites.net")
					};
					card.Links.Add(check);
				}
				context.SaveChanges();
			}

			//Checklists
			if (!context.Checklists.Any())
			{
				foreach (var card in context.Cards)
				{
					Checklist check = new Checklist()
					{
						CardId = card.Id,
						Title = "Checklist Example"
					};
					card.Checklists.Add(check);
				}
				context.SaveChanges();
			}

			//Checklist Items
			if (!context.ChecklistItems.Any())
			{
				foreach (var list in context.Checklists)
				{
					int itemAmount = random.Next(5, 10);
					for (int j = 0; j < itemAmount; j++)
					{
						ChecklistItem checkItem = new ChecklistItem()
						{
							ChecklistId = list.Id,
							Item = $"Demo item {j}",
							IsChecked = (random.Next(1, 20) > 10)
						};
						list.ChecklistItems?.Add(checkItem);
					}
				}
				context.SaveChanges();
			}

			if (!context.Activities.Any())
			{
				foreach (var card in context.Cards)
				{
					card.Activities.Add(new Activity()
					{
						CardId = card.Id,
						Author = context.Users.ToArray()[random.Next(0, context.Users.Count() - 1)],
						Created = DateTime.Now.AddDays(random.Next(-15, -2)),
						IsEdited = false,
						IsRecord = true,
						Content = $"added this card to {card.Listing.Title}"
					});
				}
				context.SaveChanges();
			}
			//CustomFields
			if (!context.CustomFields.Any())
			{

				context.CustomFields.AddRange(
				new CustomField
				{
					Name = "CustomString",
					FieldDataType = FieldDataType.String
				},
				new CustomField
				{
					Name = "CustomInt",
					FieldDataType = FieldDataType.Int
				}, new CustomField
				{
					Name = "CustomFloat",
					FieldDataType = FieldDataType.Float
				});
				context.SaveChanges();
			}


			//Reports

			if (!context.Reports.Any())
			{
				context.Reports.AddRange
					(
						new Report()
						{
							Name = "Report example 1",
							IsArchived = false,
							Filters = new List<ReportFilter>(),
							Operations = new List<ReportOperation>(),
							Results = new List<ReportResult>()
						},
						new Report()
						{
							Name = "Report example 2",
							IsArchived = false,
							Filters = new List<ReportFilter>(),
							Operations = new List<ReportOperation>(),
							Results = new List<ReportResult>()
						},
						new Report()
						{
							Name = "Report example 3",
							IsArchived = false,
							Filters = new List<ReportFilter>(),
							Operations = new List<ReportOperation>(),
							Results = new List<ReportResult>()
						}
					);

				context.SaveChanges();
			}


			if (!context.ReportFilters.Any())
			{
					context.ReportFilters.AddRange(
						new ReportFilter()
						{
							Name = "By Archived Status",
							ReportId = 1,
							Type = ReportFilterType.FromValue("ByArchive"),
							Values = "false"

							
						},
						new ReportFilter()
						{
							Name = "By Board",
							ReportId = 2,
							Type = ReportFilterType.FromValue("ByBoard"),
							Values = "1"
						},
						new ReportFilter()
						{

							Name = "By Listing",
							ReportId = 3,
							Type = ReportFilterType.FromValue("ByListing"),
							Values="1"
						}
					);;

					context.SaveChanges();
			}
		}

		static string LoremIpsum(int minWords, int maxWords,
		int minSentences, int maxSentences,
		int numParagraphs)
		{

			var words = new[]{"lorem", "ipsum", "dolor", "sit", "amet", "consectetuer",
		"adipiscing", "elit", "sed", "diam", "nonummy", "nibh", "euismod",
		"tincidunt", "ut", "laoreet", "dolore", "magna", "aliquam", "erat"};

			var rand = new Random();
			int numSentences = rand.Next(maxSentences - minSentences)
				+ minSentences + 1;
			int numWords = rand.Next(maxWords - minWords) + minWords + 1;

			StringBuilder result = new StringBuilder();

			for (int p = 0; p < numParagraphs; p++)
			{
				for (int s = 0; s < numSentences; s++)
				{
					for (int w = 0; w < numWords; w++)
					{
						if (w > 0) { result.Append(" "); }
						result.Append(words[rand.Next(words.Length)]);
					}
					result.Append(". ");
				}
			}

			return result.ToString();
		}
	}
}