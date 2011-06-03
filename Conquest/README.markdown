Conquest
========
A mini utility to track user achiements on your site  
Conquest uses a very simple DB-schema. It uses a data-access tool called [Massive](https://github.com/robconery/massive) made by [Rob Conery](http://blog.wekeroad.com/).

### Features
A very simple utilty to give achiements to your users, and by that way make the invest in your site, and think that its fun!

* Tracks manuevers (actions), that you can define, and that you execute in your code.
* Give the user Medallions (achievements, badges) - just just define what manuevers a medallion requires!
* Ability to recalculate medallions - so if you add a new medallion later, that uses old manuevers, then you can give medallions for old actions also.
* Tracks points (EXP), that you can use to level up your user.
* Gives you a easy way to get the new medallions that the user just got, so you can tell them!

### Requirements
* .NET Framework 4
* A application to use it in!
* SQL Server (maybe it will work in other databases, but it's not tested)

### How to install it?
Conquest is just a single C# code file. 

* Copy Conquest.cs and Massive.cs in your application.
* Execute the schema.sql script into the database that you wish to use.
* Ensure that you have a connectionstring in your web.config file
* Add the following code to your global.asax file (in Application_Start)
	Battlefield.Current = new Battlefield("connectionstringname");

### Setup
Now is the time to look into what you are requiring the users to do, to get a medallion (achievement, badge etc.).  
Things that a user must do, to get a medallion is called a manuever. A Manuever can be "Created a post", or "Visited the forum"
A Medallion is then a requirement-set of manuevers that a user must do. You can have a medallion for "Created 10 posts". That medallion will then have a requirement on "10 x Created a Post"

### Add a Manuever
Just under the line we added in the global.asax file:

	Battlefield.Current.AddManeuver("created_post",10);

So we just added a "created_post" maneuver. The 10 is the amount of points (Experience) given to the user, when the user performs this action.  

### Add a Medallion
In yet a new line in the global.asax, we will define your medallion:
	Battlefield.Current.AddMedallion("created_10_posts", false, new { created_post = 10 });
What we just did was to setup a new medallion with the requirement of 10 created_post manuevers. The "false" is set to not allow the medallion to be given to the same user multiple times.  
So when the person has created 10 posts, then the user will not be given yet another "Created 10 Posts" medallion.

### Action, please!
So we need to track that a user has done something. The user has executed a manuever.  
To do this, we first need to get the player from the Battlefield:

	var player = Battlefield.Current.GetPlayer("MyUser");

This will give you a nice lille class, that executes stuff on "MyUser". Remmember that "MyUser" is a username-key - so it must never change! It is recommented to use the ID column in your user database for this.  
Now we will just say this, just after the person has created his post:

	player.ExecuteManeuver("created_post");

Thats it! Now the system will automaticly give the person the medallion when he has created 10 posts. Easy, huh?

### Getting stuff out
So you don't believe that the person really got the medallion?
Then lets get the medallions on the user then!
For that we need the GetMedallionOverview method on the player.

	var overview = player.GetMedallionOverview();

The overview not contains a list of objects with two properties in them: TypeKey, and Amount.  
So to write out all the medallions, we can just write this (assumes that you are using a ASPX page):

	<% foreach(var m in Overview) { %>
		<li><%=m.TypeKey %> (<%=m.Amount %>)</li>
	<% } %>

This will give you the output "created_10_posts (1)", then the user has created the 10 posts.  

### Levels and Points
Another feature of Conquest, is Levels calculated from points. We will like to be able to tell a player: "You are in lvl 10".  
To do just that, we need to add this code in the global.asax file:
	Battlefield.Current.DefineLevels(LevelCreator.CreateLinearLevels(100,1000));
This will create a new level for every 10 points, up to level 100.  
To display the users level, you just take the player class and call 

	player.GetLevel()

You can get the points as well by calling 

	player.GetPoints() 

### Achievement popup
If you want to display something to the user, when they just got a medal, then there is a function for that!
	player.GetNewMedallions()
This will give you a dictionary, where the key is the medallion, and the value is the number of medallions of that type, that was awarded since last call.  
This method will mark all the medallions as "UserNotified", so next time you call the method, it will only show the stuff the user got after the last call.