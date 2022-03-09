using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Knapcode.TorSharp;

namespace DestinyColladaGenerator
{
	//Methods for accessing the bungie.net web api
	class apiSupport
	{
		private static string apiKey = null;
		private static string apiRoot = @"https://www.bungie.net/Platform";
		/*private static TorSharpSettings settings;
		private static TorSharpProxy proxy;
		private static HttpClientHandler handler;
		private static void torSetup()
		{
			// configure
			settings = new TorSharpSettings
			{
			ZippedToolsDirectory = Path.Combine(Path.GetTempPath(), "TorZipped"),
			ExtractedToolsDirectory = Path.Combine(Path.GetTempPath(), "TorExtracted"),
			PrivoxySettings = { Port = 8118 }, //{ Port = 1337 },
			UseExistingTools = true,
			TorSettings =
			{
				SocksPort = 9150, //1338,
				ControlPort = 9151, //1339,
				ControlPassword = "foobar",
			},
			};

			// download tools
			/*await new TorSharpToolFetcher(settings, new HttpClient()).FetchAsync();

			proxy = new TorSharpProxy(settings);
			handler = new HttpClientHandler
			{
				Proxy = new WebProxy(new Uri("http://localhost:" + settings.PrivoxySettings.Port))
			};
			/*await proxy.ConfigureAndStartAsync();
			//var httpClient = new HttpClient(handler);
			//Console.WriteLine(/*await httpClient.GetStringAsync("http://api.ipify.org").Result);
		}*/
		public static /*JObject*/dynamic makeCallJson(string url)
		{
			// for (int attempts=3; attempts>0; attempts--)
			// {
			// 	try
			// 	{
					using (var client = new HttpClient())
					{	
						client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

						var response = client.GetAsync(url).Result;
						var content = response.Content.ReadAsStringAsync().Result;
						if (content.StartsWith('<') == true) return null;
						dynamic item = JsonSerializer.Deserialize<ManifestData>(content); //JObject.Parse(content);

						return item;
					}
			// 	}
			// 	catch (TaskCanceledException e)
			// 	{
			// 		Console.WriteLine($"Failed to receive a response from the server. Attempts remaining: {attempts-1}");
			// 		if (attempts == 1)
			// 		{
			// 			throw new HttpRequestException("Request timed out", e);
			// 		}
			// 		continue;
			// 	}
			// }
			// return null;
		}

		public static dynamic makeCallGear(string url, string game)
		{
			using (var client = new HttpClient())
            {	
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                var response = client.GetAsync(url).Result;
                var content = response.Content.ReadAsStringAsync().Result;
				dynamic item = null;
                if (game.Equals(""))
                    item = JsonSerializer.Deserialize<D1Shader>(content);
                else
                    item = JsonSerializer.Deserialize<D2Shader>(content);

                return item;
            }
        }

		public static string makeCallString(string url)
		{
			// for (int attempts=3; attempts>0; attempts--)
			// {
			// 	try
			// 	{
					using (var client = new HttpClient())
					{	
						client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

						var response = client.GetAsync(url).Result;
						return response.Content.ReadAsStringAsync().Result;
					}
			// 	}
			// 	catch (TaskCanceledException e)
			// 	{
			// 		Console.WriteLine($"Failed to receive a response from the server. Attempts remaining: {attempts-1}");
			// 		if (attempts == 1)
			// 		{
			// 			throw new HttpRequestException("Request timed out", e);
			// 		}
			// 		continue;
			// 	}
			// }
			// return null;
		}

		public static byte[] makeCall(string url)
		{
			// for (int attempts=3; attempts>0; attempts--)
			// {
			// 	try
			// 	{
					//if (Program.useTor) proxy.GetNewIdentityAsync();
					using (var client = new HttpClient())
					{	
						client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

						var response = client.GetAsync(url).Result;
						var content = response.Content.ReadAsByteArrayAsync().Result;

						return content;
					}
			// 	}
			// 	catch (TaskCanceledException e)
			// 	{
			// 		Console.WriteLine($"Failed to receive a response from the server. Attempts remaining: {attempts-1}");
			// 		if (attempts == 1)
			// 		{
			// 			throw new HttpRequestException("Request timed out", e);
			// 		}
			// 		continue;
			// 	}
			// }
			// return null;
		}
		
		public static void updateLocalManifest()
		{
			Console.Write("Requesting latest api manifest...");
			Object manifestJson = makeCallJson(apiRoot+"/Destiny2/Manifest/");
			Console.WriteLine("Received.");
			
			Console.Write("Updating local copy...");
			using (StreamWriter manifestWriter = new StreamWriter(Path.Combine(new string[]{"Resources", "localManifest.json"})))
			{
				manifestWriter.Write(manifestJson.ToString());
			}
			Console.WriteLine("Done.");
		}

		public static void convertByHash(string game, string[] hashes = null, string fileOut = "")
		{
			//if (Program.useTor) torSetup();
			bool runConverter = true;
			while (runConverter) 
			{
				string[] itemHashes = null;
				if (hashes==null)
				{
					Console.Write("Input item hash(es) > ");
					itemHashes = Console.ReadLine().Split(" ", System.StringSplitOptions.RemoveEmptyEntries);
				}
				else itemHashes = hashes;

				bool skipMainConvert = false;
				if (itemHashes.Length>1 && Program.multipleFolderOutput)
				{
					for (int h=0; h<itemHashes.Length; h++)
						convertByHash(game, new string[]{itemHashes[h]}, fileOut);
					skipMainConvert = true;
				}

				ShaderPresets.propertyChannels = new Dictionary<uint, Channels>();
				ShaderPresets.propertyChannels.Clear();
				ShaderPresets.presets = new Dictionary<string, string>();
				ShaderPresets.channelData = new Dictionary<Channels, D2MatProps>();
				ShaderPresets.channelData.Clear();
				ShaderPresets.channelTextures = new Dictionary<Channels, D2TexturesContainer>();
				ShaderPresets.channelTextures.Clear();
				ShaderPresets.scripts = new Dictionary<string, string>();

				if (itemHashes.Length > 0 && !skipMainConvert)
				{
					if (hashes==null)
					{
						Console.Write("Output directory > ");
						fileOut = Console.ReadLine();
					}
					if (fileOut == "") fileOut = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Output");
					else fileOut = Path.GetFullPath(fileOut);

					if (!Directory.Exists(fileOut)) 
					{
						Directory.CreateDirectory(fileOut);
					}
					
					List<APIItemData> items = new List<APIItemData>();

					List<string> names = new List<string>();
					List<int> counts = new List<int>();

					foreach (string itemHash in itemHashes)
					{
						Console.Write("Calling item definition from manifest... ");
						ManifestData itemDef = null;
						// Some items have hidden entries that light.gg doesn't keep a copy of, but lowlidev does. Keeping the line for cases where this can be used.
						if (game=="2") itemDef = makeCallJson($@"https://www.light.gg/db/items/{itemHash}/?raw=2");
						//if (game=="2") itemDef = makeCallJson($@"https://lowlidev.com.au/destiny/api/gearasset/{itemHash}?destiny{game}");
						// Light.gg DDOS protection keeps causing issues...
						else
						{ 
							string message = ""; // Just to suppress the "e is unused" warning.
							try{itemDef = makeCallJson($@"https://lowlidev.com.au/destiny/api/gearasset/{itemHash}?destiny{game}");}
							catch (JsonException e) {message = e.Message;}
							// Ignore the error, itemDef stays as null due to it and it works as it should.
						}

						if (itemDef == null) {Console.WriteLine("Item not found. Skipping."); continue;}
						if (itemDef.gearAsset.ToString() == "false") {Console.WriteLine("Item is not marked as a gearasset. May be classified in this tool's manifest."); continue;}
						Console.WriteLine("Done.");
						
						APIItemData itemContainers = new APIItemData();
						
						List<byte[]> geometryContainers = new List<byte[]>();
						List<byte[]> textureContainers = new List<byte[]>();
						string itemName = (game == "2") ? itemDef.definition.GetProperty("displayProperties").GetProperty("name").GetString() : itemDef.definition.GetProperty("itemName").GetString();
						string itemType = (game == "2") ? itemDef.definition.GetProperty("itemTypeDisplayName").GetString() : itemDef.definition.GetProperty("itemTypeName").GetString();
						uint itemBucket = (game == "2") ? itemDef.definition.GetProperty("inventory").GetProperty("bucketTypeHash").GetUInt32() : itemDef.definition.GetProperty("bucketTypeHash").GetUInt32();
						itemContainers.type = itemType;
						if (itemType == "Shader")
						{
							Console.WriteLine("Found shader. Skipping.");
							continue;
						}
						itemContainers.bucket = itemBucket;

						if (Program.multipleFolderOutput)
							WriteCollada.multiOutItemName = itemName;
						//if (itemDef.gearAsset.GetProperty("content").GetArrayLength() < 1)
						//{
						//	Console.WriteLine($"{itemName} has no 3D content associated with it. Skipping.");
						//	continue;
						//}

						int nameIndex = names.IndexOf(itemName);
						if (nameIndex == -1)
						{
							names.Add(itemName);
							counts.Add(0);
						}
						else
						{
							counts[nameIndex]++;
							itemName += "-"+counts[nameIndex];
						}

						if(itemDef.gearAsset.content.Length > 0)
						{
							string[] geometries = itemDef.gearAsset.content[0].geometry;
							bool tG = geometries != null;

							string[] textures = itemDef.gearAsset.content[0].textures;
							bool tT = textures != null;
							
							if (itemDef.gearAsset.content[0].region_index_sets != null)
							{
								for (int g=0; g<geometries.Length; g++)
								{
									byte[] geometryContainer = makeCall($@"https://www.bungie.net/common/destiny{game}_content/geometry/platform/mobile/geometry/{geometries[g]}");
									geometryContainers.Add(geometryContainer);
								}
								
								for (int t=0; t<textures.Length; t++)
								{
									byte[] textureContainer = makeCall($@"https://www.bungie.net/common/destiny{game}_content/geometry/platform/mobile/textures/{textures[t]}");
									textureContainers.Add(textureContainer);
								}
								
								itemContainers.geometry = geometryContainers.ToArray();
								itemContainers.texture = textureContainers.ToArray();
								itemContainers.name = itemName;
								items.Add(itemContainers);
							}
							else if ((itemDef.gearAsset.content[0].female_index_set!=null) && (itemDef.gearAsset.content[0].male_index_set!=null))
							{
								IndexSet mSet = itemDef.gearAsset.content[0].male_index_set;
								IndexSet fSet = itemDef.gearAsset.content[0].female_index_set;
								
								for (int index=0; index<mSet.geometry.Length; index++)
								{
									int g = mSet.geometry[index];
									if (fSet.geometry.Contains(g)) continue;
									byte[] geometryContainer = makeCall($@"https://www.bungie.net/common/destiny{game}_content/geometry/platform/mobile/geometry/{geometries[g]}");
									geometryContainers.Add(geometryContainer);
								}
								
								for (int t=0; t<textures.Length; t++)
								{
									byte[] textureContainer = makeCall($@"https://www.bungie.net/common/destiny{game}_content/geometry/platform/mobile/textures/{textures[t]}");
									textureContainers.Add(textureContainer);
								}
								
								itemContainers.geometry = geometryContainers.ToArray();
								itemContainers.texture = textureContainers.ToArray();
								itemContainers.name = "Male_"+itemName;
								items.Add(itemContainers);
								
								
								
								APIItemData itemContainersFemale = new APIItemData();
								itemContainersFemale.type = itemType;
								List<byte[]> geometryContainersFemale = new List<byte[]>();
								List<byte[]> textureContainersFemale = new List<byte[]>();
								
								for (int index=0; index<fSet.geometry.Length; index++)
								{
									int g = fSet.geometry[index];
									if (mSet.geometry.Contains(g)) continue;
									byte[] geometryContainer = makeCall($@"https://www.bungie.net/common/destiny{game}_content/geometry/platform/mobile/geometry/{geometries[g]}");
									geometryContainersFemale.Add(geometryContainer);
								}
								
								for (int t=0; t<textures.Length; t++)
								{
									byte[] textureContainer = makeCall($@"https://www.bungie.net/common/destiny{game}_content/geometry/platform/mobile/textures/{textures[t]}");
									textureContainersFemale.Add(textureContainer);
								}
								
								itemContainersFemale.geometry = geometryContainersFemale.ToArray();
								itemContainersFemale.texture = textureContainersFemale.ToArray();
								itemContainersFemale.name = "Female_"+itemName;
								items.Add(itemContainersFemale);
							}
							else if (tG == false && textures.Length != 0)
							{
								for (int t=0; t<textures.Length; t++)
								{
									byte[] textureContainer = makeCall($@"https://www.bungie.net/common/destiny{game}_content/geometry/platform/mobile/textures/{textures[t]}");
									textureContainers.Add(textureContainer);
								}
								
								itemContainers.geometry = geometryContainers.ToArray();
								itemContainers.texture = textureContainers.ToArray();
								itemContainers.name = itemName;
								items.Add(itemContainers);
							}
							else
							{
								Console.WriteLine(itemName + " has no geometry or textures, or is missing a gendered index set.");
							}
						}
						else Console.WriteLine("Item has no content. Skipping geometry and textures.");
						ShaderPresets.propertyChannels.Clear();
						ShaderPresets.channelData.Clear();
						ShaderPresets.channelTextures.Clear();
						ShaderPresets.generatePresets(game, itemDef, itemName);
					}
					Converter.Convert(items.ToArray(), fileOut, game);
				}
				else if (skipMainConvert)
					{}
				else
					Console.WriteLine("No hashes given.");

				while (true) 
				{
					if (hashes != null) { runConverter = false; break; }
					Console.Write("Convert another file? (Y/N) ");
					string runAgain = "";
					runAgain = Console.ReadLine();

					if (runAgain.ToUpper() == "Y") 
					{
						break;
					}
					else if (runAgain.ToUpper() == "N") 
					{
						runConverter = false;
						break;
					}
					else 
					{
						Console.WriteLine("Invalid input");
					}
				}
			}
		}

	public class contentManifest
	{
		public contentManifestEntry[] entries { get; set; }
	}

	public class contentManifestEntry
	{
		public int id { get; set; }
		public string json { get; set; }
	}
	
	public static void convertContentManifest(string game)
		{
			//if (Program.useTor) torSetup();
			Console.Write("Manifest location > ");
			string manifestLocation = Console.ReadLine();
			string manifestContent = File.ReadAllText(manifestLocation);
			dynamic manifestData = JsonSerializer.Deserialize<contentManifest>(manifestContent);

			Console.Write("Starting hash > ");
			int startHash = Convert.ToInt32(Console.ReadLine());

			Console.Write("Output directory > ");
			string fileOut = Console.ReadLine();
			if (fileOut == "") fileOut = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Output");
			else fileOut = Path.GetFullPath(fileOut);

			if (!Directory.Exists(fileOut)) 
			{
				Directory.CreateDirectory(fileOut);
			}

			string failHashes = "";
			for (int h=startHash; h<manifestData.entries.Length; h++)
			{
				string itemHash = unchecked((uint) manifestData.entries[h].id).ToString();
				try
				{
					convertByHash(game, new string[]{itemHash}, fileOut);
				} catch {
					failHashes += itemHash;
				}
			}
			File.WriteAllText(Path.Combine(fileOut, "FailedHashes.txt"), failHashes);
		}

		public static void customCall()
		{
			Console.Write("Key > ");
			string userKey = Console.ReadLine();
			if (userKey != "") apiKey = userKey;

			Console.Write("Call > ");
			string callContent = Console.ReadLine();

			Console.Write("Output name > ");
			string fileOut = Console.ReadLine();
			if (fileOut == "") fileOut = "Response.txt";

			byte[] callResponse = makeCall(callContent);

			using (BinaryWriter output = new BinaryWriter(File.Open(fileOut, FileMode.Create)))
			{
				output.Write(callResponse);
			}

			Console.WriteLine("Done.");
		}
	}
}