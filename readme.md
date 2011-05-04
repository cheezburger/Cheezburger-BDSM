Cheezburger Better Database Schema Manager - Cheezburger BDSM
=====

Cheezburger BDSM is a way to easily manage schema versioning and
migrations.

Features
----

* Schema Versioning
* Simple Schema Description via XML
* Use Custom SQL for Special Cases

Example XML Schema File
======

				<?xml version="1.0" encoding="utf-8"?>
				<schema xmlns="http://schemas.icanhascheezburger.com/db" version="52">
				  <tables>
					<table name="Category">
					  <callback method="FillCategories" type="Mine.Utility.Schema.Populators, MineCore" />
					  <columns>
						<column name="CategoryId" type="int" isIdentity="true" />
						<column name="CategoryName" type="varchar" length="50" nullable="true" />
						<column name="Rank" type="int" nullable="true"/>
						<column name="DisplayTemplateGallery" type="bit" default="0"/>
					  </columns>
					  <indexes>
						<index name="PK_Category" type="PrimaryKey" columns="CategoryId" />
					  </indexes>
					</table>
				  </tables>
				</schema>

				
